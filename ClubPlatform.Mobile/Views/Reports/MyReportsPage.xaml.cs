
using System;
using System.Collections.Generic;
using System.Linq;
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Reports;
using Microsoft.Maui.Controls;

namespace ClubPlatform.Mobile.Views.Reports;

public partial class MyReportsPage : ContentPage
{
    private readonly IApiService _apiService;

    public MyReportsPage(IApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadReportsAsync();
    }

    // Загружает жалобы, отправленные текущим пользователем
    private async Task LoadReportsAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var reports = await _apiService.GetAsync<List<ReportDto>>(
                "api/reports/my",
                CancellationToken.None);

            var items = reports?
                .Select(report => new MyReportItem
                {
                    Id = report.Id,
                    ClubName = report.ClubName,
                    MessageAuthorName = report.MessageAuthorName,
                    MessageContent = report.MessageContent,
                    Reason = report.Reason,
                    Status = report.Status,
                    CreatedAt = report.CreatedAt
                })
                .ToList() ?? new List<MyReportItem>();

            ReportsCollection.ItemsSource = items;
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось загрузить мои жалобы: {ex.Message}",
                "ОК");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}
