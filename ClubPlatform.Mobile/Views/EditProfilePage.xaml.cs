
using System.Globalization;
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Auth;
using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Mobile.Views;

public partial class EditProfilePage : ContentPage
{
    private readonly IApiService _apiService;

    private UserProfileResponse? _profile;

    private byte[]? _photoBytes;
    private string? _photoFileName;
    private string? _photoContentType;

    private bool _isBusy;

    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".webp" };

    public EditProfilePage(IApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        GenderPicker.ItemsSource = new[]
        {
            "Мужской",
            "Женский",
            "Другой"
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_profile is null)
        {
            await LoadProfileAsync();
        }
    }

    /// <summary>
    /// Загружает текущие данные в поля формы.
    /// </summary>
    private async Task LoadProfileAsync()
    {
        try
        {
            SaveButton.IsEnabled = false;
            StatusLabel.Text = "Загружаем данные...";

            _profile = await _apiService.GetAsync<UserProfileResponse>(
                "api/Auth/me");

            if (_profile is null)
            {
                throw new InvalidOperationException(
                    "Сервер не вернул данные профиля.");
            }
            NameEntry.Text = _profile.Name;

            AgeEntry.Text = _profile.Age > 0
                ? _profile.Age.ToString(CultureInfo.InvariantCulture)
                : string.Empty;

            GenderPicker.SelectedItem = _profile.Gender switch
            {
                "Male" => "Мужской",
                "Female" => "Женский",
                "Other" => "Другой",
                _ => null
            };

            ShowPhoto(_profile.PhotoUrl);

            StatusLabel.Text = "Данные загружены.";
            SaveButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить профиль.";

            await DisplayAlert(
                "Ошибка загрузки",
                ex.Message,
                "Понятно");
        }
    }

    /// <summary>
    /// Отображает уже сохранённую фотографию.
    /// </summary>
    private void ShowPhoto(string? url)
    {
        if (Uri.TryCreate(
            url,
            UriKind.Absolute,
            out var photoUri)
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
    /// Выбирает изображение с устройства.
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
                    PickerTitle = "Выбери фотографию",
                    FileTypes = FilePickerFileType.Images
                });

            if (result is null)
                return;

            var extension = Path.GetExtension(result.FileName)
                .ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                await DisplayAlert(
                    "Неподдерживаемый формат",
                    "Выбери фотографию JPG, JPEG, PNG или WebP.",
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

            PhotoPreview.Source = ImageSource.FromStream(
                () => new MemoryStream(_photoBytes));

            StatusLabel.Text = "Новая фотография выбрана.";
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
    /// Проверяет форму, загружает фото при необходимости
    /// и сохраняет новые данные через API.
    /// </summary>
    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        if (_isBusy || _profile is null)
            return;

        var name = NameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            await DisplayAlert(
                "Проверь имя",
                "Имя должно содержать не менее двух символов.",
                "Понятно");
            return;
        }

        if (!short.TryParse(AgeEntry.Text, out var age)
            || age < 0
            || age > 120)
        {
            await DisplayAlert(
                "Проверь возраст",
                "Укажи целое число от 0 до 120.",
                "Понятно");

            return;
        }

        if (GenderPicker.SelectedItem is not string genderText)
        {
            await DisplayAlert(
                "Выбери пол",
                "Выбери значение в списке пола.",
                "Понятно");

            return;
        }

        var gender = genderText switch
        {
            "Мужской" => Gender.Male,
            "Женский" => Gender.Female,
            "Другой" => Gender.Other,
            _ => (Gender)(-1)
        };

        if (!Enum.IsDefined(gender))
        {
            await DisplayAlert(
                "Ошибка",
                "Выбрано недопустимое значение пола.",
                "Понятно");

            return;
        }

        _isBusy = true;
        SaveButton.IsEnabled = false;
        StatusLabel.Text = "Сохраняем изменения...";

        try
        {
            // Если выбрана новая фотография, загружаем её
            // в Cloudinary через защищённый endpoint.
            var photoUrl = _profile.PhotoUrl;

            if (_photoBytes is not null)
            {
                using var photoStream =
                    new MemoryStream(_photoBytes);

                var upload = await _apiService.PostFileAsync<ImageUploadResponse>(
                    "api/Images/upload",
                    photoStream,
                    _photoFileName ?? "profile.jpg",
                    _photoContentType ?? "image/jpeg");

                if (string.IsNullOrWhiteSpace(upload?.Url))
                {
                    throw new InvalidOperationException(
                        "Сервер не вернул ссылку на фотографию.");
                }

                photoUrl = upload.Url;
            }

            var request = new UpdateProfileRequest
            {
                Name = name,
                Age = age,
                Gender = gender,
                PhotoUrl = photoUrl
            };

            await _apiService.PutAsync<UpdateProfileRequest, System.Text.Json.JsonElement>(
                "api/Auth/me",
                request);

            await DisplayAlert(
                "Готово!",
                "Профиль успешно обновлён.",
                "Отлично");

            // Возвращаемся в профиль. При повторном появлении
            // он запросит свежие данные с сервера.
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось сохранить изменения.";

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
}
