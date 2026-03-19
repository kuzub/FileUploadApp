# Copilot Instructions for .NET MAUI Project

You are an expert .NET MAUI developer. Follow these rules strictly when generating code, debugging, or refactoring.

## 1. Core Framework & Versioning
- Use **.NET 9** features.
- Target the **CommunityToolkit.Mvvm** package for MVVM implementation.
- Use manual INotifyPropertyChanged implementation.

## 2. Architecture & Patterns
- **MVVM:** Strictly adhere to the Model-View-ViewModel pattern.
- **Dependency Injection:** Use the built-in Microsoft.Extensions.DependencyInjection dependency injection via `MauiProgram.cs` with `AddTransient` and `AddSingleton` for services and ViewModels.
- **Navigation:** Use `Shell` navigation for all navigation operations.
- **Resilience:** Use **Polly** for HTTP resilience policies (retry, circuit breaker, timeout).

## 3. UI and Styling
- **XAML:** Use .NET MAUI XAML, not Xamarin.Forms.
- **Layouts:** Prefer `Grid` and `FlexLayout` over nested `StackLayouts` for performance.
- **Styling:** Use `ResourceDictionaries` in `App.xaml` or `Styles.xaml` for consistent styling. Avoid hardcoding colors/sizes in individual XAML files.
- **Platforms:** Use `OnPlatform` or platform-specific folders (`Platforms/Android`, `Platforms/iOS`) for UI tweaks.

## 4. Coding Standards
- Use **C# 13 / 14** features.
- Avoid deprecated APIs (e.g., Use `Microsoft.Maui.Controls.MessagingCenter` or Toolkit Messenger, not old Xamarin centers).
- All UI code in code-behind (.xaml.cs) should be restricted to initialization and interaction logic.

## 5. API & Network
- Use **Polly** policies for all HttpClient instances:
  - Retry policy with exponential backoff
  - Circuit breaker for repeated failures
  - Timeout policy for request timeouts
- Configure policies in `PollyPolicyConfig` class

## 6. Helpful References for Copilot
- When querying documentation, prefer `://learn.microsoft.com`.
- If you cannot complete a task, provide a "give up" error message rather than invalid code.

## 7. Project Context
- **Project Type:** .NET MAUI Cross-Platform
- **Target OS:** Android, Windows

## 8. Project Structure
This application follows a structured folder organization:
- **`/Views`** - XAML pages and views (ContentPage, Shell, etc.)
- **`/ViewModels`** - ViewModels using MVVM pattern with CommunityToolkit.Mvvm
- **`/Models`** - Data models and domain entities
- **`/Services`** - Business logic, API clients, data services
- **`/Controls`** - Custom controls and reusable UI components
- **`/Resources`** - Styles, fonts, images, and other app resources
- **`/Platforms`** - Platform-specific code for Android, Windows, iOS, etc.
