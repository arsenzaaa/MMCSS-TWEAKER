using System.ComponentModel;
using System.Globalization;

namespace MMCSSTweaker.Core;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private readonly Dictionary<string, (string En, string Ru)> _strings = new(StringComparer.Ordinal)
    {
        ["Header_Subtitle"] = (
            "Low-latency MMCSS registry tweaker • System Profile / Audio Tasks",
            "Low-latency MMCSS registry tweaker • System Profile / Audio Tasks"),
        ["Header_TelegramPrefix"] = ("Telegram: ", "Telegram: "),
        ["Header_LanguageTooltip"] = ("Switch language", "Сменить язык"),
        ["Button_Apply"] = ("APPLY", "APPLY"),
        ["Button_AutoOptimize"] = ("AUTO-OPTIMIZATIONS", "AUTO-OPTIMIZATIONS"),
        ["Button_Refresh"] = ("REFRESH", "REFRESH"),
        ["Button_RestoreBackup"] = ("RESTORE BACKUP", "RESTORE BACKUP"),
        ["Section_SystemProfile"] = ("SYSTEM PROFILE", "SYSTEM PROFILE"),
        ["Section_AudioTasks"] = ("AUDIO TASKS", "AUDIO TASKS"),

        ["Group_GeneralSettings"] = ("General Settings", "General Settings"),
        ["System_MaxThreadsPerProcess_Help"] = (
            "(8-128) Maximum number of MMCSS threads registered per process (out-of-range values fall back to 32)",
            "(8-128) Максимальное количество зарегистрированных в MMCSS потоков с одного процесса (значения вне диапазона сводятся к 32)"),
        ["System_MaxThreadsPerProcess_Desc"] = (
            "(8-128) Maximum number of MMCSS threads registered per process (out-of-range values fall back to 32)",
            "(8-128) Максимальное количество зарегистрированных в MMCSS потоков с одного процесса (значения вне диапазона сводятся к 32)"),
        ["System_MaxThreadsTotal_Help"] = (
            "(64-65535) Maximum number of MMCSS threads registered (out-of-range values fall back to 256)",
            "(64-65535) Максимальное количество зарегистрированных в MMCSS потоков (значения вне диапазона сводятся к 256)"),
        ["System_MaxThreadsTotal_Desc"] = (
            "(64-65535) Maximum number of MMCSS threads registered (out-of-range values fall back to 256)",
            "(64-65535) Максимальное количество зарегистрированных в MMCSS потоков (значения вне диапазона сводятся к 256)"),
        ["System_NetworkUnlimited"] = ("Disabled (FFFFFFFF)", "Отключен (FFFFFFFF)"),
        ["System_NetworkThrottlingIndex_Help"] = (
            "(1-70 or FFFFFFFF) Throttling mechanism that limits packets per millisecond and enables the NDIS thread that offloads work from DPC (0 is treated as 1; FFFFFFFF disables the mechanism and the NDIS thread)",
            "(1-70 или FFFFFFFF) Механизм тротлинга, который ограничивает количество пакетов в миллисекунду и активирует поток NDIS, который забирает работу с уровня DPC (0 трактуется как 1; FFFFFFFF отключает механизм и поток NDIS)"),
        ["System_NetworkThrottlingIndex_Desc"] = (
            "(1-70 or FFFFFFFF) Throttling mechanism that limits packets per millisecond and enables the NDIS thread that offloads work from DPC (0 is treated as 1; FFFFFFFF disables the mechanism and the NDIS thread)",
            "(1-70 или FFFFFFFF) Механизм тротлинга, который ограничивает количество пакетов в миллисекунду и активирует поток NDIS, который забирает работу с уровня DPC (0 трактуется как 1; FFFFFFFF отключает механизм и поток NDIS)"),
        ["Reason_NetworkThrottling_Unlimited"] = (
            "FFFFFFFF disables the throttling mechanism and the NDIS thread",
            "FFFFFFFF отключает механизм троттлинга и поток NDIS"),
        ["System_SchedulerTimerResolution_Help"] = (
            "(1-10000) MMCSS timer resolution in 100-ns units (values above 10000 are clamped to 10000)",
            "(1-10000) Timer resolution MMCSS в 100-ns единицах (значения выше 10000 принудительно становятся 10000)"),
        ["System_SchedulerTimerResolution_Desc"] = (
            "(1-10000) MMCSS timer resolution in 100-ns units (values above 10000 are clamped to 10000)",
            "(1-10000) Timer resolution MMCSS в 100-ns единицах (значения выше 10000 принудительно становятся 10000)"),
        ["System_NoLazyMode_Help"] = (
            "NoLazyMode: 0 = Lazy Mode enabled, 1 = Lazy Mode disabled. Side effect with value 1: the audiodg audio thread sometimes gets priority 7 (and the application's audio thread gets priority 8) if Scheduling Category is not set to Low",
            "NoLazyMode: 0 = Lazy Mode включен, 1 = Lazy Mode отключен. Побочный эффект при значении 1: аудио поток audiodg иногда получает приоритет 7 (аудио поток приложения получает приоритет 8), если Scheduling Category не стоит на Low"),
        ["System_NoLazyMode_Desc"] = (
            "NoLazyMode: 0 = Lazy Mode enabled, 1 = Lazy Mode disabled. Side effect with value 1: the audiodg audio thread sometimes gets priority 7 (and the application's audio thread gets priority 8) if Scheduling Category is not set to Low",
            "NoLazyMode: 0 = Lazy Mode включен, 1 = Lazy Mode отключен. Побочный эффект при значении 1: аудио поток audiodg иногда получает приоритет 7 (аудио поток приложения получает приоритет 8), если Scheduling Category не стоит на Low"),
        ["System_SchedulerPeriod_Help"] = (
            "(50000-1000000) SchedulerPeriod (100 ns) is the MMCSS scheduler base period. The percentage is defined by SystemResponsiveness: that portion goes to SleepResponsiveness (sleep event), the rest to Realtime (sleep event). Also defines the interval between IdleDetection events (out-of-range values fall back to 100000).",
            "(50000-1000000) SchedulerPeriod (100 нс) — базовый период планировщика MMCSS. Процент задается параметром SystemResponsiveness: эта доля уходит на SleepResponsiveness (событие сна), остальная часть — Realtime (событие сна). Также определяет интервал между событиями IdleDetection (значения вне диапазона сводятся к 100000)."),
        ["System_SchedulerPeriod_Desc"] = (
            "(50000-1000000) SchedulerPeriod (100 ns) is the MMCSS scheduler base period. The percentage is defined by SystemResponsiveness: that portion goes to SleepResponsiveness (sleep event), the rest to Realtime (sleep event). Also defines the interval between IdleDetection events (out-of-range values fall back to 100000).",
            "(50000-1000000) SchedulerPeriod (100 нс) — базовый период планировщика MMCSS. Процент задается параметром SystemResponsiveness: эта доля уходит на SleepResponsiveness (событие сна), остальная часть — Realtime (событие сна). Также определяет интервал между событиями IdleDetection (значения вне диапазона сводятся к 100000)."),
        ["System_SystemResponsiveness_Help"] = (
            "(10-100, rounded down to tens) Percentage of SchedulerPeriod that goes to SleepResponsiveness. Value 100 disables MMCSS. Values above 50 prevent audio threads from staying registered in MMCSS (their priority is locked to 15)",
            "(10-100, округляется в меньшую сторону до десятков) Процент от SchedulerPeriod, который уходит в SleepResponsiveness. Значение 100 отключает MMCSS. Значения выше 50 не дают аудио потокам удерживать регистрацию в MMCSS (их приоритет фиксируется на 15)"),
        ["System_SystemResponsiveness_Desc"] = (
            "(10-100, rounded down to tens) Percentage of SchedulerPeriod that goes to SleepResponsiveness. Value 100 disables MMCSS. Values above 50 prevent audio threads from staying registered in MMCSS (their priority is locked to 15)",
            "(10-100, округляется в меньшую сторону до десятков) Процент от SchedulerPeriod, который уходит в SleepResponsiveness. Значение 100 отключает MMCSS. Значения выше 50 не дают аудио потокам удерживать регистрацию в MMCSS (их приоритет фиксируется на 15)"),
        ["Group_LazyModeSettings"] = ("Lazy Mode Settings", "Lazy Mode Settings"),
        ["System_IdleDetectionCycles_Help"] = (
            "(1-31) Number of IdleDetection events that must pass to enter Lazy Mode / use IdleDetectionLazy events (out-of-range values fall back to 2)",
            "(1-31) Количество событий IdleDetection, которое должно пройти для входа в Lazy Mode / использования событий IdleDetectionLazy (значения вне диапазона сводятся к 2)"),
        ["System_IdleDetectionCycles_Desc"] = (
            "(1-31) Number of IdleDetection events that must pass to enter Lazy Mode / use IdleDetectionLazy events (out-of-range values fall back to 2)",
            "(1-31) Количество событий IdleDetection, которое должно пройти для входа в Lazy Mode / использования событий IdleDetectionLazy (значения вне диапазона сводятся к 2)"),
        ["System_LazyModeTimeout_Help"] = (
            "(0-FFFFFFFF, 100 ns units) Interval between IdleDetectionLazy events (if DWORD is missing, default is 1000000)",
            "(0-FFFFFFFF, 100 нс единицы) Интервал между событиями IdleDetectionLazy (если DWORD отсутствует, значение по умолчанию 1000000)"),
        ["System_LazyModeTimeout_Desc"] = (
            "(0-FFFFFFFF, 100 ns units) Interval between IdleDetectionLazy events (if DWORD is missing, default is 1000000)",
            "(0-FFFFFFFF, 100 нс единицы) Интервал между событиями IdleDetectionLazy (если DWORD отсутствует, значение по умолчанию 1000000)"),

        ["Reason_LazyMode_Disabled"] = (
            "NoLazyMode = 1 disables Lazy Mode: IdleDetection and LazyModeTimeout are not used",
            "NoLazyMode = 1 отключает Lazy Mode: IdleDetection и LazyModeTimeout не используются"),
        ["Reason_LazyMode_Unset"] = (
            "Select NoLazyMode to change Lazy Mode settings",
            "Выберите NoLazyMode, чтобы изменить параметры Lazy Mode"),
        ["Group_AudioTaskSettings"] = ("Audio Task Settings", "Audio Task Settings"),
        ["Audio_Affinity_Help"] = (
            "Affinity mask for threads that are part of the audio task (0/FFFFFFFF = not used)",
            "Affinity mask для потоков, которые входят в аудио задачу (0/FFFFFFFF = Не используется)"),
        ["Audio_Affinity_Desc"] = (
            "Affinity mask for threads that are part of the audio task (0/FFFFFFFF = not used)",
            "Affinity mask для потоков, которые входят в аудио задачу (0/FFFFFFFF = Не используется)"),
        ["Audio_LatencySensitive_Help"] = (
            "Whether threads that are part of the audio task are allowed to create Latency Sensitivity Hints",
            "Разрешено ли потокам, которые входят в аудио задачу, создавать Latency Sensitivity Hints"),
        ["Audio_LatencySensitive_Desc"] = (
            "Whether threads that are part of the audio task are allowed to create Latency Sensitivity Hints",
            "Разрешено ли потокам, которые входят в аудио задачу, создавать Latency Sensitivity Hints"),
        ["Audio_SchedulingCategory_Help"] = (
            "Priority category for the audio task",
            "Категория приоритетов для аудио задачи"),
        ["Audio_SchedulingCategory_Desc"] = (
            "Priority category for the audio task",
            "Категория приоритетов для аудио задачи"),
        ["Audio_Priority_Help"] = (
            "(1-8) Priority boost for audio task threads for the Medium category (Increase Priority if your audio stutters)",
            "(1-8) Буст приоритета потоков, которые входят в\u00A0аудио задачу для Medium категории (Повышайте\u00A0Priority, если ваш звук лагает)"),
        ["Audio_Priority_Desc"] = (
            "(1-8) Priority boost for audio task threads for the Medium category (Increase Priority if your audio stutters)",
            "(1-8) Буст приоритета потоков, которые входят в\u00A0аудио задачу для Medium категории (Повышайте\u00A0Priority, если ваш звук лагает)"),
        ["Audio_BackgroundPriority_Help"] = (
            "(1-8) Priority boost for audio task threads for the Low category (Increase BackgroundPriority if your audio stutters)",
            "(1-8) Буст приоритета потоков, которые входят в\u00A0аудио задачу для Low категории (Повышайте\u00A0BackgroundPriority, если ваш звук лагает)"),
        ["Audio_BackgroundPriority_Desc"] = (
            "(1-8) Priority boost for audio task threads for the Low category (Increase BackgroundPriority if your audio stutters)",
            "(1-8) Буст приоритета потоков, которые входят в\u00A0аудио задачу для Low категории (Повышайте\u00A0BackgroundPriority, если ваш звук лагает)"),
        ["Audio_PriorityWhenYielded_Help"] = (
            "(1-19) Non-boosted priority of the audiodg audio thread (Increase Priority When Yielded if your audio stutters)",
            "(1-19) Приоритет аудио потока audiodg без буста MMCSS (Повышайте Priority When Yielded, если звук лагает)"),
        ["Audio_PriorityWhenYielded_Desc"] = (
            "(1-19) Non-boosted priority of the audiodg audio thread (Increase Priority When Yielded if your audio stutters)",
            "(1-19) Приоритет аудио потока audiodg без буста MMCSS (Повышайте Priority When Yielded, если звук лагает)"),
        ["Reason_Priority_Low"] = (
            "Priority works only with Scheduling Category = Medium. For Low use Background Priority",
            "Priority работает только при Scheduling Category = Medium. Для Low используется Background Priority"),
        ["Reason_Priority_High"] = (
            "With Scheduling Category = High, Priority is not used (fixed by the system to 2)",
            "При Scheduling Category = High значение Priority не используется (фиксируется системой на 2)"),
        ["Reason_Priority_Unset"] = (
            "Select Scheduling Category to use Priority",
            "Выберите Scheduling Category, чтобы использовать Priority"),
        ["Reason_Background_NotLow"] = (
            "Background Priority is available only with Scheduling Category = Low",
            "Background Priority доступен только при Scheduling Category = Low"),
        ["Reason_Background_Unset"] = (
            "Select Scheduling Category to use Background Priority",
            "Выберите Scheduling Category, чтобы использовать Background Priority"),
        ["Reason_Yielded_Low"] = (
            "Priority When Yielded does not apply when Scheduling Category = Low",
            "Priority When Yielded не применяется при Scheduling Category = Low"),
        ["Reason_Yielded_Unset"] = (
            "Select Scheduling Category to use Priority When Yielded",
            "Выберите Scheduling Category, чтобы использовать Priority When Yielded"),
        ["Group_ThreadStatus"] = ("Thread Status", "Thread Status"),
        ["Label_MmcssThread"] = ("MMCSS thread:", "Поток MMCSS:"),
        ["Label_AudioThreadPriority"] = ("Audio thread priority:", "Приоритет потоков аудио:"),
        ["Label_NdisThread"] = ("NDIS thread:", "Поток NDIS:"),
        ["Label_NetAdapterCxThreads"] = ("NetAdapterCx threads (Rx/Tx):", "Потоки NetAdapterCx (Rx/Tx):"),
        ["Status_Inactive"] = ("Inactive", "Неактивен"),
        ["Status_ActivePriority"] = ("Active (Priority {0})", "Активен (Приоритет {0})"),
        ["Status_MmcssActiveFixed"] = ("Active (Priority 27)", "Активен (Приоритет 27)"),
        ["Status_NdisActiveFixed"] = ("Active (Priority 8)", "Активен (Приоритет 8)"),
        ["Status_AudioNoBoost"] = ("{0} (No MMCSS boost: {1})", "{0} (Без буста MMCSS: {1})"),
        ["Group_NdisSettings"] = ("NDIS Settings", "NDIS Settings"),
        ["Ndis_ReceiveWorkerThreadPriority_Help"] = (
            "(8 or 16-31) NDIS worker thread priority (9-15 do not apply to the active network worker thread; it stays at 8)",
            "(8 или 16-31) Приоритет потока NDIS (значения 9-15 не применяются к активному network worker thread: он остается на 8)"),
        ["Ndis_ReceiveWorkerThreadPriority_Desc"] = (
            "(8 or 16-31) NDIS worker thread priority (9-15 do not apply to the active network worker thread; it stays at 8)",
            "(8 или 16-31) Приоритет потока NDIS (значения 9-15 не применяются к активному network worker thread: он остается на 8)"),

        ["Reason_Ndis_NetAdapterCx"] = (
            "NetAdapterCx detected: ReceiveWorkerThreadPriority does not affect Rx/Tx threads",
            "Обнаружен NetAdapterCx: ReceiveWorkerThreadPriority не влияет на Rx/Tx потоки"),
        ["Reason_Ndis_Low"] = (
            "Scheduling Category = Low disables the MMCSS/NDIS thread",
            "Scheduling Category = Low отключает поток MMCSS/NDIS"),
        ["Reason_Ndis_Unset"] = (
            "Select Scheduling Category to activate the NDIS thread",
            "Выберите Scheduling Category, чтобы активировать NDIS thread"),
        ["Reason_Ndis_ThrottlingDisabled"] = (
            "NetworkThrottlingIndex = FFFFFFFF disables the NDIS thread",
            "NetworkThrottlingIndex = FFFFFFFF отключает поток NDIS"),
        ["Msg_TelegramOpenFailed"] = ("Failed to open Telegram.\nLink: {0}", "Не удалось открыть Telegram.\nСсылка: {0}"),
        ["Title_Telegram"] = ("Telegram", "Telegram"),
        ["Title_Success"] = ("Success", "Успешно"),
        ["Title_Error"] = ("Error", "Ошибка"),
        ["Title_Backup"] = ("Backup", "Бекап"),
        ["Msg_ApplyAllSuccess"] = ("Settings applied successfully!\nRestart your PC for full effect.", "Настройки применены успешно!\nПерезагрузите ПК для полного применения."),
        ["Msg_ApplyAllPartial"] = ("Some settings couldn't be applied.\nRun as Administrator and try again.", "Некоторые настройки не удалось применить.\nЗапусти от администратора и попробуй снова."),
        ["Title_AutoOptimization"] = ("Auto-Optimization", "Авто-Оптимизация"),
        ["Msg_AutoOptimizeAllSuccess"] = ("Auto-optimization completed successfully!\nRestart your PC for full effect.", "Авто-оптимизация выполнена успешно!\nПерезагрузите ПК для полного применения."),
        ["Msg_AutoOptimizeAllPartial"] = ("Auto-optimization completed partially.\nSome settings couldn't be applied.", "Авто-оптимизация выполнена частично.\nНекоторые настройки не удалось применить."),
        ["Msg_RestoreBackupSuccess"] = ("Backup restored successfully!\nRestart your PC for full effect.", "Бекап восстановлен!\nПерезагрузите ПК для полного применения."),
        ["Msg_RestoreBackupFailed"] = ("Failed to restore backup.\n\n{0}", "Не удалось восстановить бекап.\n\n{0}"),
        ["Msg_SystemProfileApplied"] = ("System Profile settings applied successfully!", "Настройки System Profile применены успешно!"),
        ["Msg_SystemProfileApplyFailed"] = ("Some System Profile settings couldn't be applied.", "Некоторые настройки System Profile не удалось применить"),
        ["Msg_SystemProfileOptimized"] = ("System Profile settings optimized for maximum performance!", "Настройки System Profile оптимизированы для максимальной производительности!"),
        ["Msg_ApplyFailedGeneric"] = ("Some settings couldn't be applied.", "Некоторые настройки не удалось применить"),
        ["Msg_AudioOptimized"] = ("Audio task settings optimized for maximum performance!", "Настройки аудио задачи оптимизированы для максимальной производительности!"),
        ["Msg_AudioApplyFailed"] = ("Some audio task settings couldn't be applied.", "Некоторые настройки аудио задачи не удалось применить"),
        ["Msg_AudioApplied"] = ("Audio task thread settings applied successfully!", "Настройки потоков, которые входят в аудио задачу применены успешно!"),
        ["Msg_AudioThreadsApplyFailed"] = ("Some audio task thread settings couldn't be applied.", "Некоторые настройки потоков, которые входят в аудио задачу не удалось применить"),

        ["Msg_AdminRequired"] = ("Administrator privileges are required to apply settings.", "Нужны права администратора для применения настроек."),
        ["Msg_FatalError"] = ("Application startup/runtime error.\n\n{0}: {1}\n\nLog: {2}", "Ошибка запуска/работы приложения.\n\n{0}: {1}\n\nЛог: {2}"),
        ["Cli_AutoOptimizeSuccess"] = ("Auto-optimization completed successfully!", "Авто-Оптимизация выполнена успешно!"),
        ["Cli_AutoOptimizeSuccessDetails"] = (
            "System and audio settings have been optimized for low latency.",
            "Системные и аудио настройки оптимизированы для низкой задержки."),
        ["Cli_AutoOptimizePartial"] = ("Auto-optimization completed partially", "Авто-Оптимизация выполнена частично"),
        ["Cli_SystemProfileApplyFailed"] = (" - Failed to fully apply System Profile settings", " - Не удалось полностью применить настройки System Profile"),
        ["Cli_AudioApplyFailed"] = (" - Failed to fully apply audio task thread settings", " - Не удалось полностью применить настройки потоков, которые входят в аудио задачу"),
        ["Cli_RunGuiForMore"] = ("Please run the GUI version for more control", "Запусти GUI версию для более тонкой настройки"),
        ["Cli_InvalidArgs"] = ("Invalid arguments. Use --help for usage information.", "Неверные аргументы. Используй --help для справки."),
        ["Cli_UsageTitle"] = ("MMCSS-TWEAKER", "MMCSS-TWEAKER"),
        ["Cli_Usage"] = ("Usage:", "Использование:"),
        ["Cli_Options"] = ("Options:", "Опции:"),
        ["Cli_OptionAutoOptimize"] = ("  --auto-optimize  Apply auto-optimization for maximum performance", "  --auto-optimize  Применить авто-оптимизацию для максимальной производительности"),
        ["Cli_OptionGui"] = ("  --gui            Start GUI (default)", "  --gui            Запустить графический интерфейс (default)"),
    };

    private UiLanguage _language = UiLanguage.En;

    private LocalizationManager()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiLanguage Language
    {
        get => _language;
        set
        {
            if (_language == value)
                return;

            _language = value;

            OnPropertyChanged(nameof(Language));
            OnPropertyChanged(nameof(ToggleLanguageLabel));
            OnPropertyChanged(nameof(ToggleLanguageTooltip));
            OnPropertyChanged("Item[]");
        }
    }

    public string ToggleLanguageLabel => _language == UiLanguage.En ? "RU" : "EN";

    public string ToggleLanguageTooltip => this["Header_LanguageTooltip"];

    public string this[string key] => Get(key);

    public string Format(string key, params object[] args)
    {
        string format = Get(key);
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    private string Get(string key)
    {
        if (_strings.TryGetValue(key, out (string En, string Ru) value))
            return _language == UiLanguage.En ? value.En : value.Ru;

        return key;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
