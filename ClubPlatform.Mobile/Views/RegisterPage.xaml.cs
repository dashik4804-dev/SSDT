using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using ClubPlatform.Shared.DTOs.Auth;
using ClubPlatform.Shared.Enums;
//using Java.Nio.FileNio.Attributes;

namespace ClubPlatform.Mobile.Views;

/// <summary>
/// Страница регистрации пользователя.
/// </summary>
public partial class RegisterPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;

    // Выбранная фотография хранится в памяти,
    // пока пользователь не отправит форму.
    private byte[]? _photoBytes;

    private string? _photoFileName;
    private string? _photoContentType;

    private bool _isProcessing;

    public RegisterPage(
        IApiService apiService,
        ITokenStorage tokenStorage)
    {
        InitializeComponent();

        _apiService = apiService;
        _tokenStorage = tokenStorage;
    }

    /// <summary>
    /// Открывает выбор изображения на устройстве.
    /// </summary>
    private async void OnChoosePhotoClicked(
        object? sender,
        EventArgs e)
    {
        try
        {
            var photo = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Выбери фотографию профиля",
                    FileTypes = FilePickerFileType.Images
                });

            // Пользователь мог отменить выбор.
            if (photo is null)
            {
                return;
            }

            var extension = Path.GetExtension(photo.FileName)
                .ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                await DisplayAlert(
                    "Формат не поддерживается",
                    "Выбери изображение JPG, PNG или WebP.",
                    "Понятно");

                return;
            }

            await using var inputStream =
                await photo.OpenReadAsync();

            using var memoryStream = new MemoryStream();

            await inputStream.CopyToAsync(memoryStream);

            if (memoryStream.Length == 0)
            {
                await DisplayAlert(
                    "Ошибка",
                    "Выбранный файл пустой.",
                    "Понятно");

                return;
            }

            // Максимальный размер изображения — 10 МБ.
            const long maxFileSize = 10 * 1024 * 1024;

            if (memoryStream.Length > maxFileSize)
            {
                await DisplayAlert(
                    "Файл слишком большой",
                    "Размер фотографии не должен превышать 10 МБ.",
                    "Понятно");

                return;
            }

            _photoBytes = memoryStream.ToArray();
            _photoFileName = photo.FileName;

            // Определяем MIME-тип по расширению файла.
            _photoContentType = extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            // Показываем предварительный просмотр.
            var selectedBytes = _photoBytes;

            PhotoPreview.Source = ImageSource.FromStream(
                () => new MemoryStream(selectedBytes));

            PhotoPreview.IsVisible = true;
            PhotoPlaceholder.IsVisible = false;

            StatusLabel.Text = "Фотография выбрана.";
            StatusLabel.TextColor = Color.FromArgb("#41734B");
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                exception.ToString());

            await DisplayAlert(
                "Ошибка выбора фотографии",
                "Не удалось открыть выбранный файл. Попробуй другое изображение.",
                "Понятно");
        }
    }

    /// <summary>
    /// Проверяет форму и создаёт пользователя.
    /// Сначала загружает фотографию, затем регистрирует аккаунт.
    /// </summary>
    private async void OnRegisterClicked(
        object? sender,
        EventArgs e)
    {
        if (_isProcessing)
        {
            return;
        }

        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmPassword =
            ConfirmPasswordEntry.Text ?? string.Empty;

        // -------------------------------------------------
        // ПРОВЕРЯЕМ ФОРМУ
        // -------------------------------------------------

        if (_photoBytes is null ||
            string.IsNullOrWhiteSpace(_photoFileName) ||
            string.IsNullOrWhiteSpace(_photoContentType))
        {
            StatusLabel.Text = "Сначала выбери фотографию профиля.";
            return;
        }

        if (name.Length < 2)
        {
            StatusLabel.Text = "Имя должно содержать минимум 2 символа.";
            return;
        }

        if (!short.TryParse(AgeEntry.Text, out var age) ||
            age < 1 ||
            age > 120)
        {
            StatusLabel.Text = "Введи корректный возраст от 1 до 120.";
            return;
        }

        if (GenderPicker.SelectedIndex < 0)
        {
            StatusLabel.Text = "Выбери пол.";
            return;
        }

        if (string.IsNullOrWhiteSpace(email) ||
            !email.Contains('@') ||
            !email.Contains('.'))
        {
            StatusLabel.Text = "Введи корректный email.";
            return;
        }

        if (password.Length < 8)
        {
            StatusLabel.Text = "Пароль должен содержать минимум 8 символов.";
            return;
        }

        if (password != confirmPassword)
        {
            StatusLabel.Text = "Пароли не совпадают.";
            return;
        }

        _isProcessing = true;

        RegisterButton.IsEnabled = false;
        ChoosePhotoButton.IsEnabled = false;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            // -------------------------------------------------
            // ЭТАП 1. ЗАГРУЗКА ФОТОГРАФИИ
            // -------------------------------------------------

            StatusLabel.Text = "Загружаем фотографию...";

            ImageUploadResponse? uploadResponse;

            await using (var fileStream =
                new MemoryStream(_photoBytes))
            {
                uploadResponse =
                    await _apiService.PostFileAsync<ImageUploadResponse>(
                        "api/Images/registration-upload",
                        fileStream,
                        _photoFileName,
                        _photoContentType);
            }

            if (uploadResponse is null ||
                string.IsNullOrWhiteSpace(uploadResponse.Url))
            {
                throw new InvalidOperationException(
                    "Сервер не вернул ссылку на фотографию.");
            }

            // -------------------------------------------------
            // ЭТАП 2. ОПРЕДЕЛЯЕМ ПОЛ
            // -------------------------------------------------

            var gender = GenderPicker.SelectedIndex switch
            {
                0 => Gender.Male,
                1 => Gender.Female,
                2 => Gender.Other,
                _ => throw new InvalidOperationException(
                    "Необходимо выбрать пол.")
            };

            // -------------------------------------------------
            // ЭТАП 3. РЕГИСТРАЦИЯ
            // -------------------------------------------------

            StatusLabel.Text = "Создаём аккаунт...";

            var request = new RegisterRequest
            {
                Email = email,
                Password = password,
                Name = name,
                Age = age,
                Gender = gender,

                // Сохраняем ссылку, полученную от Cloudinary.
                PhotoUrl = uploadResponse.Url
            };

            var authResponse =
                await _apiService.PostAsync<RegisterRequest, AuthResponse>(
                    "api/Auth/register",
                    request);

            if (authResponse is null ||
                string.IsNullOrWhiteSpace(authResponse.Token))
            {
                throw new InvalidOperationException(
                    "Сервер не вернул токен авторизации.");
            }

            // -------------------------------------------------
            // ЭТАП 4. СОХРАНЯЕМ JWT
            // -------------------------------------------------

            await _tokenStorage.SaveTokenAsync(
                authResponse.Token);

            _apiService.SetBearerToken(
                authResponse.Token);

            StatusLabel.Text = "Регистрация успешно завершена.";
            StatusLabel.TextColor = Color.FromArgb("#41734B");

            await DisplayAlert(
                "Добро пожаловать!",
                $"Аккаунт создан, {authResponse.User.Name}!\n\n" +
                "Фотография профиля загружена, а данные сохранены. " +
                "Авторизация выполнена успешно.",
                "Продолжить");

            // Пока экран клубов ещё не создан.
            // Возвращаемся на Welcome, сохраняя JWT.
            await Navigation.PopToRootAsync();
        }
        catch (HttpRequestException exception)
        {
            System.Diagnostics.Debug.WriteLine(
                exception.ToString());

            if (exception.Message.Contains("409"))
            {
                StatusLabel.Text =
                    "Пользователь с таким email уже зарегистрирован.";
            }
            else if (exception.Message.Contains("400"))
            {
                StatusLabel.Text =
                    "Сервер отклонил данные. Проверь поля и фотографию.";
            }
            else
            {
                StatusLabel.Text =
                    "Не удалось связаться с сервером. " +
                    "Проверь подключение и попробуй ещё раз.";
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                exception.ToString());

            StatusLabel.Text =
                "Не удалось завершить регистрацию. Попробуй ещё раз.";
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            RegisterButton.IsEnabled = true;
            ChoosePhotoButton.IsEnabled = true;

            _isProcessing = false;
        }
    }

    /// <summary>
    /// Возвращает пользователя к Welcome.
    /// </summary>
    private async void OnBackClicked(
        object? sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}