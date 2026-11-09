using System;
using KPSSGeography.Ads;
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
        AdPolicy ads;
        ExamModeController exam;
        StudyScreen study;
        BossScreen boss;

        public void Initialize(CityQuizGame quizGame, QuizHud quizHud, TitleScreen titleScreen, LevelSelectScreen levelSelect,
            LevelCatalog levelCatalog, ProgressStore progressStore, AdPolicy adPolicy, ExamModeController examController,
            StudyScreen studyScreen, BossScreen bossScreen)
        {
            study = studyScreen;
            boss = bossScreen;
            ads = adPolicy;
            exam = examController;
            game = quizGame;
            hud = quizHud;
            title = titleScreen;
            levels = levelSelect;
            catalog = levelCatalog;
            progress = progressStore;

            title.PlayRequested += () => ShowLevels(null);
            title.StudyRequested += ShowStudy;
            title.BossRequested += PlayBoss;
            boss.ExitRequested += ShowTitle;
            boss.RetryRequested += PlayBoss;
            study.BackRequested += ShowTitle;
            levels.BackRequested += ShowTitle;
            levels.PlayRequested += Play;

            hud.LevelsRequested += () => AfterResults(() => ShowLevels(game.Level));
            hud.RetryRequested += () => AfterResults(() => Play(game.Level));
            hud.NextRequested += () => AfterResults(() =>
            {
                var next = catalog.NextPlayable(game.Level);
                if (next != null && progress.IsUnlocked(next)) Play(next);
                else ShowLevels(game.Level);
            });
            hud.RequestRewarded = done => ads.ShowRewarded("continue", done);
            game.CanOfferContinue = () => ads.CanOfferContinue;
            game.GameEnded += _ => ads.OnLevelFinished();
            hud.NextAvailable = level =>
            {
                var next = catalog.NextPlayable(level);
                return next != null && progress.IsUnlocked(next);
            };

            ShowTitle();
        }

        /// <summary>Sonuç ekranından ayrılırken uygunsa geçiş reklamı gösterir (duraklatma menüsünden ayrılışta göstermez).</summary>
        void AfterResults(Action next)
        {
            if (game.State == QuizState.Finished) ads.BeforeLeavingResults(next);
            else next();
        }

        void ShowTitle()
        {
            study.Hide();
            boss.Hide();
            LeaveGame();
            levels.Hide();
            exam.SetLook(false, false); // giriş ekranında harita her zaman renkli
            title.Show();
        }

        /// <summary>Boss savaşı: iki raund (Fay Hattı, İller); ekranı BossScreen, akışı CityQuizGame yürütür.</summary>
        void PlayBoss()
        {
            LeaveGame();
            boss.Hide();
            title.Hide();
            levels.Hide();
            study.Hide();
            exam.SetLook(false, false); // boss savaşında harita her zaman renkli
            game.StartLevel(BossScreen.Level);
        }

        /// <summary>Eğitim: soru yok; konular soldan seçilir, harita sağda adlarıyla gösterilir.</summary>
        void ShowStudy()
        {
            LeaveGame();
            title.Hide();
            levels.Hide();
            exam.SetLook(false, false); // eğitimde harita her zaman renkli
            study.Open();
        }

        void ShowLevels(LevelDefinition focus)
        {
            LeaveGame();
            title.Hide();
            exam.SetLook(GameSettings.ExamMode, false);
            levels.Open(focus);
        }

        void Play(LevelDefinition level)
        {
            if (level == null) return;
            title.Hide();
            levels.Hide();
            exam.SetLook(GameSettings.ExamMode, false);
            game.StartLevel(level);
        }

        void LeaveGame()
        {
            if (hud.IsVisible) hud.Hide();
            game.Abort();
        }
    }
}
