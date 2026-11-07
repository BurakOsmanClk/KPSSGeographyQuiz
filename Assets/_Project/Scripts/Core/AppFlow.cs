using KPSSGeography.Meta;
using KPSSGeography.Quiz;
using KPSSGeography.UI;
using UnityEngine;

namespace KPSSGeography.Core
{
    /// <summary>
    /// Ekranlar arası akış: Giriş → Bölümler → Oyun → Sonuç → (Tekrar | Sonraki | Bölümler).
    /// Harita her ekranda arka planda durur. Menülerde kamera vitrin modunda süzülür.
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        CityQuizGame game;
        QuizHud hud;
        TitleScreen title;
        LevelSelectScreen levels;
        LevelCatalog catalog;
        ProgressStore progress;

        public void Initialize(CityQuizGame quizGame, QuizHud quizHud, TitleScreen titleScreen, LevelSelectScreen levelSelect,
            LevelCatalog levelCatalog, ProgressStore progressStore)
        {
            game = quizGame;
            hud = quizHud;
            title = titleScreen;
            levels = levelSelect;
            catalog = levelCatalog;
            progress = progressStore;

            title.PlayRequested += () => ShowLevels(null);
            levels.BackRequested += ShowTitle;
            levels.PlayRequested += Play;

            hud.LevelsRequested += () => ShowLevels(game.Level);
            hud.RetryRequested += () => Play(game.Level);
            hud.NextRequested += () =>
            {
                var next = catalog.NextPlayable(game.Level);
                if (next != null && progress.IsUnlocked(next)) Play(next);
                else ShowLevels(game.Level);
            };
            hud.NextAvailable = level =>
            {
                var next = catalog.NextPlayable(level);
                return next != null && progress.IsUnlocked(next);
            };

            ShowTitle();
        }

        void ShowTitle()
        {
            LeaveGame();
            levels.Hide();
            title.Show();
        }

        void ShowLevels(LevelDefinition focus)
        {
            LeaveGame();
            title.Hide();
            levels.Open(focus);
        }

        void Play(LevelDefinition level)
        {
            if (level == null) return;
            title.Hide();
            levels.Hide();
            game.StartLevel(level);
        }

        void LeaveGame()
        {
            if (hud.IsVisible) hud.Hide();
            game.Abort();
        }
    }
}
