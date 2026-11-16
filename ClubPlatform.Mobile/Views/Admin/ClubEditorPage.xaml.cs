using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Clubs;
using ClubPlatform.Shared.DTOs.ClubSuggestions;

namespace ClubPlatform.Mobile.Views.Admin;

public partial class ClubEditorPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ClubListItem? _club;
    private readonly ClubSuggestionDto? _suggestion;

    // Сохраняем ответ создания, чтобы при ошибке привязки
    // повторно не создавать такой же клуб.
    private ClubDto? _createdClubFromSuggestion;

    private readonly List<InterestOption> _interestOptions = new();

    private byte[]? _photoBytes;
    private string? _photoFileName;
    private string? _photoContentType;

    private bool _isBusy;
    private bool _isInitialized;

    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".webp" };

    public ClubEditorPage(
    IApiService apiService,
    ClubListItem? club = null,
    ClubSuggestionDto? suggestion = null)
    {
        InitializeComponent();

        _apiService = apiService;
        _club = club;
        _suggestion = suggestion;

        PageTitle.Text = club is not null
            ? "Редактирование клуба"
            : suggestion is not null
                ? "Создание клуба по предложению"
                : "Создание клуба";

        SaveButton.Text = club is null
            ? "Создать клуб"
            : "Сохранить изменения";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isInitialized)
        {
            await LoadEditorAsync();
        }
    }

    /// <summary>
    /// Заполняет поля клуба и загружает доступные интересы.
    /// </summary>
    private async Task LoadEditorAsync()
    {
        try
        {
            StatusLabel.Text = "Загружаем данные...";
            SaveButton.IsEnabled = false;

            if (_club is not null)
            {
                NameEntry.Text = _club.Name;
                DescriptionEditor.Text = _club.Description;
                AddressEntry.Text = _club.Address;
                PhoneEntry.Text = _club.ContactPhone;
                EmailEntry.Text = _club.ContactEmail;

                MaxMembersEntry.Text =
                    _club.MaxMembers.HasValue && _club.MaxMembers.Value > 0
                        ? _club.MaxMembers.Value.ToString()
                        : string.Empty;

                ShowPhoto(_club.PhotoUrl);
            }

            else if (_suggestion is not null)
            {
                NameEntry.Text = _suggestion.Name;
                DescriptionEditor.Text = _suggestion.Description;

                // Подставляем предполагаемый адрес, если пользователь его указал.
                AddressEntry.Text = _suggestion.SuggestedAddress ?? string.Empty;
            }

            var interests =
                await _apiService.GetAsync<List<InterestListItem>>(
                    "api/Interests")
                ?? new List<InterestListItem>();

            _interestOptions.Clear();
            InterestsLayout.Children.Clear();

            foreach (var interest in interests)
            {
                var option = new InterestOption
                {
                    Id = interest.Id,
                    Name = interest.Name,
                    Emoji = interest.Emoji,
                    IsSelected =
                        _club?.Interests.Any(
                            clubInterest => clubInterest.Id == interest.Id) == true
                        ||
                        (_suggestion is not null
                            && string.Equals(
                                _suggestion.InterestName,
                                interest.Name,
                                StringComparison.OrdinalIgnoreCase))
                };

                _interestOptions.Add(option);

                var checkBox = new CheckBox
                {
                    IsChecked = option.IsSelected,
                    VerticalOptions = LayoutOptions.Center,
                    Color = Color.FromArgb("#8A6448")
                };

                // Сохраняем выбор в объекте конкретного интереса.
                checkBox.CheckedChanged += (_, args) =>
                {
                    option.IsSelected = args.Value;
                };

                var nameLabel = new Label
                {
                    Text = option.DisplayName,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#55402F"),
                    FontSize = 15
                };

                var row = new HorizontalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Center
                };

                row.Children.Add(checkBox);
                row.Children.Add(nameLabel);

                InterestsLayout.Children.Add(row);
            }

            _isInitialized = true;
            StatusLabel.Text = "Данные загружены.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить данные.";

            await DisplayAlert(
                "Ошибка",
                $"Не удалось загрузить интересы или данные клуба.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Показывает уже сохранённую фотографию.
    /// </summary>
    private void ShowPhoto(string? url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var photoUri)
            && (photoUri.Scheme == Uri.UriSchemeHttp
                || photoUri.Scheme == Uri.UriSchemeHttps))
        {
            PhotoPreview.Source = ImageSource.FromUri(photoUri);
        }
        else
        {
            PhotoPreview.Source = null;
        }
    }

    /// <summary>
    /// Выбирает новую фотографию клуба.
    /// Сам файл загружается в Cloudinary при сохранении формы.
    /// </summary>
    private async void OnChoosePhotoClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Выбери фотографию клуба",
                    FileTypes = FilePickerFileType.Images
                });

            if (result is null)
                return;

            var extension =
                Path.GetExtension(result.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                await DisplayAlert(
                    "Формат не поддерживается",
                    "Выбери JPG, JPEG, PNG или WebP.",
                    "Понятно");

                return;
            }

            await using var input = await result.OpenReadAsync();
            using var memory = new MemoryStream();

            await input.CopyToAsync(memory);

            if (memory.Length > 10 * 1024 * 1024)
            {
                await DisplayAlert(
                    "Файл слишком большой",
                    "Размер фотографии не должен превышать 10 МБ.",
                    "Понятно");

                return;
            }

            _photoBytes = memory.ToArray();
            _photoFileName = Path.GetFileName(result.FileName);

            _photoContentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var bytes = _photoBytes;

            PhotoPreview.Source = ImageSource.FromStream(
                () => new MemoryStream(bytes));

            StatusLabel.Text = "Фотография выбрана.";
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка выбора фотографии",
                ex.Message,
                "Понятно");
        }
    }

    /// <summary>
    /// Проверяет форму и создаёт новый клуб либо сохраняет изменения.
    /// </summary>
    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        if (_isBusy)
            return;

        var name = NameEntry.Text?.Trim();
        var description = DescriptionEditor.Text?.Trim();
        var address = AddressEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            await DisplayAlert(
                "Проверь название",
                "Название должно содержать не менее двух символов.",
                "Понятно");
            return;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            await DisplayAlert(
                "Добавь описание",
                "Заполни описание клуба.",
                "Понятно");
            return;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            await DisplayAlert(
                "Добавь адрес",
                "Укажи адрес клуба в Москве.",
                "Понятно");
            return;
        }

        int? maxMembers = null;

        if (!string.IsNullOrWhiteSpace(MaxMembersEntry.Text))
        {
            if (!int.TryParse(MaxMembersEntry.Text, out var parsed)
                || parsed < 1)
            {
                await DisplayAlert(
                    "Проверь число участников",
                    "Введи положительное целое число или оставь поле пустым.",
                    "Понятно");
                return;
            }

            maxMembers = parsed;
        }

        var selectedInterestIds = _interestOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Id)
            .ToList();

        _isBusy = true;
        SaveButton.IsEnabled = false;
        StatusLabel.Text = "Сохраняем клуб...";

        try
        {
            var photoUrl = _club?.PhotoUrl;

            // Загружаем новую фотографию, только если пользователь выбрал её.
            if (_photoBytes is not null)
            {
                using var stream = new MemoryStream(_photoBytes);

                var upload =
                    await _apiService.PostFileAsync<ImageUploadResponse>(
                        "api/Images/upload",
                        stream,
                        _photoFileName ?? "club.jpg",
                        _photoContentType ?? "image/jpeg");

                if (string.IsNullOrWhiteSpace(upload?.Url))
                {
                    throw new InvalidOperationException(
                        "Сервер не вернул ссылку на фотографию.");
                }

                photoUrl = upload.Url;
            }

            var phone = string.IsNullOrWhiteSpace(PhoneEntry.Text)
                ? null
                : PhoneEntry.Text.Trim();

            var email = string.IsNullOrWhiteSpace(EmailEntry.Text)
                ? null
                : EmailEntry.Text.Trim();

            if (_club is null)
            {
                // Если клуб уже создали, но связь с заявкой не сохранилась,
                // при повторном нажатии только повторяем привязку.
                if (_suggestion is not null
                    && _createdClubFromSuggestion is not null)
                {
                    await _apiService.PutAsync(
                        $"api/ClubSuggestions/{_suggestion.Id}"
                        + $"/link-club/{_createdClubFromSuggestion.Id}");
                }
                else
                {
                    var request = new CreateClubRequest
                    {
                        Name = name,
                        Description = description,
                        Address = address,
                        PhotoUrl = photoUrl,
                        ContactPhone = phone,
                        ContactEmail = email,
                        MaxMembers = maxMembers,
                        InterestIds = selectedInterestIds
                    };

                    var createdClub =
                        await _apiService.PostAsync<CreateClubRequest, ClubDto>(
                            "api/Clubs",
                            request);

                    if (createdClub is null)
                    {
                        throw new InvalidOperationException(
                            "Сервер не вернул созданный клуб.");
                    }

                    if (_suggestion is not null)
                    {
                        // Запоминаем результат до запроса привязки.
                        _createdClubFromSuggestion = createdClub;

                        await _apiService.PutAsync(
                            $"api/ClubSuggestions/{_suggestion.Id}"
                            + $"/link-club/{createdClub.Id}");
                    }
                }
            }
            else
            {
                var request = new UpdateClubRequest
                {
                    Name = name,
                    Description = description,
                    Address = address,
                    PhotoUrl = photoUrl,
                    ContactPhone = phone,
                    ContactEmail = email,
                    MaxMembers = maxMembers,
                    InterestIds = selectedInterestIds
                };

                await _apiService.PutAsync<UpdateClubRequest, ClubDto>(
                    $"api/Clubs/{_club.Id}",
                    request);
            }

            await DisplayAlert(
                "Готово!",
                _club is null
                    ? "Новый клуб создан."
                    : "Данные клуба обновлены.",
                "Отлично");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось сохранить клуб.";

            await DisplayAlert(
                "Ошибка сохранения",
                $"Проверь данные и подключение к серверу.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isBusy = false;
            SaveButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Вариант интереса с состоянием флажка.
    /// </summary>
    private sealed class InterestOption
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Emoji { get; set; } = string.Empty;

        public bool IsSelected { get; set; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(Emoji)
                ? Name
                : $"{Emoji} {Name}";
    }
}