using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BalancerAPI.Data.Data;

public class BalancerDbContext(DbContextOptions<BalancerDbContext> options) : DbContext(options)
{
    public DbSet<PlayerName> Names => Set<PlayerName>();
    public DbSet<BaseWeight> BaseWeights => Set<BaseWeight>();
    public DbSet<BaseWeightDaily> BaseWeightsDaily => Set<BaseWeightDaily>();
    public DbSet<BaseWeightWeekly> BaseWeightsWeekly => Set<BaseWeightWeekly>();
    public DbSet<BaseWeightCurrentDay> BaseWeightsCurrentDay => Set<BaseWeightCurrentDay>();
    public DbSet<BaseWeightCurrentWeek> BaseWeightsCurrentWeek => Set<BaseWeightCurrentWeek>();
    public DbSet<ExperimentalSpecLog> ExperimentalSpecLogs => Set<ExperimentalSpecLog>();
    public DbSet<ExperimentalSpecWeight> ExperimentalSpecWeights => Set<ExperimentalSpecWeight>();
    public DbSet<ExperimentalSpecWeightWeekly> ExperimentalSpecWeightsWeekly => Set<ExperimentalSpecWeightWeekly>();
    public DbSet<ExperimentalSpecWeightCurrentWeek> ExperimentalSpecWeightsCurrentWeek => Set<ExperimentalSpecWeightCurrentWeek>();
    public DbSet<ExperimentalSpecsWl> ExperimentalSpecsWl => Set<ExperimentalSpecsWl>();
    public DbSet<ExperimentalSpecsWlWeekly> ExperimentalSpecsWlWeekly => Set<ExperimentalSpecsWlWeekly>();
    public DbSet<ExperimentalSpecsWlDaily> ExperimentalSpecsWlDaily => Set<ExperimentalSpecsWlDaily>();
    public DbSet<TimeWeek> TimeWeeks => Set<TimeWeek>();
    public DbSet<TimeDay> TimeDays => Set<TimeDay>();
    public DbSet<TimeSeason> TimeSeasons => Set<TimeSeason>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<ExperimentalSpecsWlCurrentWeek> ExperimentalSpecsWlCurrentWeek => Set<ExperimentalSpecsWlCurrentWeek>();
    public DbSet<ExperimentalSpecsWlCurrentDay> ExperimentalSpecsWlCurrentDay => Set<ExperimentalSpecsWlCurrentDay>();
    public DbSet<ExperimentalBalancePlayerData> ExperimentalBalancePlayerData => Set<ExperimentalBalancePlayerData>();
    public DbSet<ExperimentalBalanceLog> ExperimentalBalanceLogs => Set<ExperimentalBalanceLog>();
    public DbSet<ExperimentalInputLog> ExperimentalInputLogs => Set<ExperimentalInputLog>();
    public DbSet<AdjustmentDaily> AdjustmentDaily => Set<AdjustmentDaily>();
    public DbSet<AdjustmentDailyLog> AdjustmentDailyLogs => Set<AdjustmentDailyLog>();
    public DbSet<AdjustmentWeeklyLog> AdjustmentWeeklyLogs => Set<AdjustmentWeeklyLog>();
    public DbSet<AdjustmentManualDailyLog> AdjustmentManualDailyLogs => Set<AdjustmentManualDailyLog>();
    public DbSet<AdjustmentManualWeeklyLog> AdjustmentManualWeeklyLogs => Set<AdjustmentManualWeeklyLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureNames(modelBuilder);
        ConfigureBaseWeights(modelBuilder);
        ConfigureBaseWeightsDaily(modelBuilder);
        ConfigureBaseWeightsWeekly(modelBuilder);
        ConfigureBaseWeightsCurrentDayView(modelBuilder);
        ConfigureBaseWeightsCurrentWeekView(modelBuilder);
        ConfigureExperimentalSpecLogs(modelBuilder);
        ConfigureExperimentalSpecWeights(modelBuilder);
        ConfigureExperimentalSpecWeightsWeekly(modelBuilder);
        ConfigureExperimentalSpecWeightsCurrentWeekView(modelBuilder);
        ConfigureExperimentalSpecsWl(modelBuilder);
        ConfigureExperimentalSpecsWlWeekly(modelBuilder);
        ConfigureExperimentalSpecsWlDaily(modelBuilder);
        ConfigureTimeWeek(modelBuilder);
        ConfigureTimeDay(modelBuilder);
        ConfigureTimeSeason(modelBuilder);
        ConfigureSettings(modelBuilder);
        ConfigureCurrentWeekView(modelBuilder);
        ConfigureCurrentDayView(modelBuilder);
        ConfigureBalancePlayerDataView(modelBuilder);
        ConfigureExperimentalBalanceLog(modelBuilder);
        ConfigureExperimentalInputLog(modelBuilder);
        ConfigureAdjustmentDaily(modelBuilder);
        ConfigureAdjustmentDailyLog(modelBuilder);
        ConfigureAdjustmentWeeklyLog(modelBuilder);
        ConfigureAdjustmentManualDailyLog(modelBuilder);
        ConfigureAdjustmentManualWeeklyLog(modelBuilder);
    }

    private static void ConfigureAdjustmentManualWeeklyLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdjustmentManualWeeklyLog>(entity =>
        {
            entity.ToTable("adjustment_manual_weekly_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.Spec).HasColumnName("spec").HasMaxLength(32);
            entity.Property(e => e.PreviousOffset).HasColumnName("previous_offset");
            entity.Property(e => e.NewOffset).HasColumnName("new_offset");
            entity.Property(e => e.BaseWeight).HasColumnName("base_weight");
            entity.Property(e => e.PreviousSpecWeight).HasColumnName("previous_spec_weight");
            entity.Property(e => e.NewSpecWeight).HasColumnName("new_spec_weight");
            entity.Property(e => e.Date).HasColumnName("date").HasColumnType("timestamp with time zone");
        });
    }

    private static void ConfigureAdjustmentManualDailyLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdjustmentManualDailyLog>(entity =>
        {
            entity.ToTable("adjustment_manual_daily_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.PreviousWeight).HasColumnName("previous_weight");
            entity.Property(e => e.NewWeight).HasColumnName("new_weight");
            entity.Property(e => e.Date).HasColumnName("date").HasColumnType("timestamp with time zone");
        });
    }

    private static void ConfigureAdjustmentWeeklyLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdjustmentWeeklyLog>(entity =>
        {
            entity.ToTable("adjustment_weekly_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.WeekKey).HasColumnName("week_key");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.Spec).HasColumnName("spec").HasMaxLength(32);
            entity.Property(e => e.Wins).HasColumnName("wins");
            entity.Property(e => e.Losses).HasColumnName("losses");
            entity.Property(e => e.Adjusted).HasColumnName("adjusted");
            entity.Property(e => e.PreviousWeight).HasColumnName("previous_weight");
            entity.Property(e => e.PreviousOffset).HasColumnName("previous_offset");
            entity.Property(e => e.Date).HasColumnName("date").HasColumnType("timestamp with time zone");
        });
    }

    private static void ConfigureAdjustmentDailyLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdjustmentDailyLog>(entity =>
        {
            entity.ToTable("adjustment_daily_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.PreviousWeight).HasColumnName("previous_weight");
            entity.Property(e => e.NewWeight).HasColumnName("new_weight");
            entity.Property(e => e.Date).HasColumnName("date").HasColumnType("timestamp with time zone");
        });
    }

    private static void ConfigureAdjustmentDaily(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdjustmentDaily>(entity =>
        {
            entity.ToTable("adjustment_daily");
            entity.HasKey(e => e.Uuid);
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.Trajectory).HasColumnName("trajectory");
        });
    }

    private static void ConfigureExperimentalBalanceLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalBalanceLog>(entity =>
        {
            entity.ToTable("experimental_balance_log");
            entity.HasKey(e => e.BalanceId);
            entity.Property(e => e.BalanceId).HasColumnName("balance_id").HasColumnType("uuid");
            entity.Property(e => e.GameId).HasColumnName("game_id").HasMaxLength(24).IsFixedLength().IsRequired(false);
            entity.Property(e => e.Balance).HasColumnName("balance").HasColumnType("jsonb");
            entity.Property(e => e.Meta).HasColumnName("meta").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            entity.Property(e => e.Posted).HasColumnName("posted");
            entity.Property(e => e.Input).HasColumnName("input").HasColumnType("jsonb").IsRequired(false);
            entity.Property(e => e.Counted).HasColumnName("counted");
        });
    }

    private static void ConfigureExperimentalInputLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalInputLog>(entity =>
        {
            entity.ToTable("experimental_input_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.BalanceId).HasColumnName("balance_id").HasColumnType("uuid");
            entity.Property(e => e.GameId).HasColumnName("game_id").HasMaxLength(24).IsFixedLength();
            entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(32);
            entity.Property(e => e.OccurredAt).HasColumnName("date").HasColumnType("timestamp with time zone");
        });
    }

    private static void ConfigureNames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerName>(entity =>
        {
            entity.ToTable("names");
            entity.HasKey(e => e.Uuid);
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(16);
            entity.Property(e => e.PreviousNames)
                .HasColumnName("previous_names")
                .HasColumnType("character varying(16)[]")
                .HasDefaultValueSql("'{}'::character varying[]");
        });
    }

    private static void ConfigureBaseWeights(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaseWeight>(entity =>
        {
            entity.ToTable("base_weights");
            entity.HasKey(e => e.Uuid);
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.LastUpdated)
                .HasColumnName("last_updated")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        });
    }

    private static void ConfigureBaseWeightsDaily(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaseWeightDaily>(entity =>
        {
            entity.ToTable("base_weights_daily");
            entity.HasKey(e => new { e.Uuid, e.DayStartDate });
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.DayStartDate).HasColumnName("day_start_date");
            entity.Property(e => e.Weight).HasColumnName("weight");
        });
    }

    private static void ConfigureBaseWeightsWeekly(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaseWeightWeekly>(entity =>
        {
            entity.ToTable("base_weights_weekly");
            entity.HasKey(e => new { e.Uuid, e.WeekStartDate });
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.WeekStartDate).HasColumnName("week_start_date");
            entity.Property(e => e.Weight).HasColumnName("weight");
        });
    }

    private static void ConfigureBaseWeightsCurrentDayView(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaseWeightCurrentDay>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("base_weights_current_day");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.CurrentWeight).HasColumnName("current_weight");
            entity.Property(e => e.PreviousWeight).HasColumnName("previous_weight");
            entity.Property(e => e.DayStartDate).HasColumnName("day_start_date");
            entity.Property(e => e.WeightChange).HasColumnName("weight_change");
        });
    }

    private static void ConfigureBaseWeightsCurrentWeekView(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaseWeightCurrentWeek>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("base_weights_current_week");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.CurrentWeight).HasColumnName("current_weight");
            entity.Property(e => e.PreviousWeight).HasColumnName("previous_weight");
            entity.Property(e => e.WeekStartDate).HasColumnName("week_start_date");
            entity.Property(e => e.WeightChange).HasColumnName("weight_change");
        });
    }

    private static void ConfigureExperimentalSpecLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecLog>(entity =>
        {
            entity.ToTable("experimental_spec_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.BalanceId).HasColumnName("balance_id").HasColumnType("uuid");
            entity.Property(e => e.Pyromancer).HasColumnName("pyromancer").HasColumnType("uuid");
            entity.Property(e => e.Cryomancer).HasColumnName("cryomancer").HasColumnType("uuid");
            entity.Property(e => e.Aquamancer).HasColumnName("aquamancer").HasColumnType("uuid");
            entity.Property(e => e.Berserker).HasColumnName("berserker").HasColumnType("uuid");
            entity.Property(e => e.Defender).HasColumnName("defender").HasColumnType("uuid");
            entity.Property(e => e.Revenant).HasColumnName("revenant").HasColumnType("uuid");
            entity.Property(e => e.Avenger).HasColumnName("avenger").HasColumnType("uuid");
            entity.Property(e => e.Crusader).HasColumnName("crusader").HasColumnType("uuid");
            entity.Property(e => e.Protector).HasColumnName("protector").HasColumnType("uuid");
            entity.Property(e => e.Thunderlord).HasColumnName("thunderlord").HasColumnType("uuid");
            entity.Property(e => e.Spiritguard).HasColumnName("spiritguard").HasColumnType("uuid");
            entity.Property(e => e.Earthwarden).HasColumnName("earthwarden").HasColumnType("uuid");
            entity.Property(e => e.Assassin).HasColumnName("assassin").HasColumnType("uuid");
            entity.Property(e => e.Vindicator).HasColumnName("vindicator").HasColumnType("uuid");
            entity.Property(e => e.Apothecary).HasColumnName("apothecary").HasColumnType("uuid");
            entity.Property(e => e.Conjurer).HasColumnName("conjurer").HasColumnType("uuid");
            entity.Property(e => e.Sentinel).HasColumnName("sentinel").HasColumnType("uuid");
            entity.Property(e => e.Luminary).HasColumnName("luminary").HasColumnType("uuid");
        });
    }

    private static void ConfigureExperimentalSpecWeights(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecWeight>(entity =>
        {
            entity.ToTable("experimental_spec_weights");
            entity.HasKey(e => e.Uuid);
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.PyromancerOffset).HasColumnName("pyromancer_offset");
            entity.Property(e => e.CryomancerOffset).HasColumnName("cryomancer_offset");
            entity.Property(e => e.AquamancerOffset).HasColumnName("aquamancer_offset");
            entity.Property(e => e.BerserkerOffset).HasColumnName("berserker_offset");
            entity.Property(e => e.DefenderOffset).HasColumnName("defender_offset");
            entity.Property(e => e.RevenantOffset).HasColumnName("revenant_offset");
            entity.Property(e => e.AvengerOffset).HasColumnName("avenger_offset");
            entity.Property(e => e.CrusaderOffset).HasColumnName("crusader_offset");
            entity.Property(e => e.ProtectorOffset).HasColumnName("protector_offset");
            entity.Property(e => e.ThunderlordOffset).HasColumnName("thunderlord_offset");
            entity.Property(e => e.SpiritguardOffset).HasColumnName("spiritguard_offset");
            entity.Property(e => e.EarthwardenOffset).HasColumnName("earthwarden_offset");
            entity.Property(e => e.AssassinOffset).HasColumnName("assassin_offset");
            entity.Property(e => e.VindicatorOffset).HasColumnName("vindicator_offset");
            entity.Property(e => e.ApothecaryOffset).HasColumnName("apothecary_offset");
            entity.Property(e => e.ConjurerOffset).HasColumnName("conjurer_offset");
            entity.Property(e => e.SentinelOffset).HasColumnName("sentinel_offset");
            entity.Property(e => e.LuminaryOffset).HasColumnName("luminary_offset");
            entity.Property(e => e.LastUpdated)
                .HasColumnName("last_updated")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        });
    }

    private static void ConfigureExperimentalSpecWeightsWeekly(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecWeightWeekly>(entity =>
        {
            entity.ToTable("experimental_spec_weights_weekly");
            entity.HasKey(e => new { e.Uuid, e.WeekStartDate });
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.WeekStartDate).HasColumnName("week_start_date");
            entity.Property(e => e.PyromancerOffset).HasColumnName("pyromancer_offset");
            entity.Property(e => e.CryomancerOffset).HasColumnName("cryomancer_offset");
            entity.Property(e => e.AquamancerOffset).HasColumnName("aquamancer_offset");
            entity.Property(e => e.BerserkerOffset).HasColumnName("berserker_offset");
            entity.Property(e => e.DefenderOffset).HasColumnName("defender_offset");
            entity.Property(e => e.RevenantOffset).HasColumnName("revenant_offset");
            entity.Property(e => e.AvengerOffset).HasColumnName("avenger_offset");
            entity.Property(e => e.CrusaderOffset).HasColumnName("crusader_offset");
            entity.Property(e => e.ProtectorOffset).HasColumnName("protector_offset");
            entity.Property(e => e.ThunderlordOffset).HasColumnName("thunderlord_offset");
            entity.Property(e => e.SpiritguardOffset).HasColumnName("spiritguard_offset");
            entity.Property(e => e.EarthwardenOffset).HasColumnName("earthwarden_offset");
            entity.Property(e => e.AssassinOffset).HasColumnName("assassin_offset");
            entity.Property(e => e.VindicatorOffset).HasColumnName("vindicator_offset");
            entity.Property(e => e.ApothecaryOffset).HasColumnName("apothecary_offset");
            entity.Property(e => e.ConjurerOffset).HasColumnName("conjurer_offset");
            entity.Property(e => e.SentinelOffset).HasColumnName("sentinel_offset");
            entity.Property(e => e.LuminaryOffset).HasColumnName("luminary_offset");
        });
    }

    private static void ConfigureExperimentalSpecWeightsCurrentWeekView(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecWeightCurrentWeek>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("experimental_spec_weights_current_week");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");

            var specs = new[]
            {
                "Pyromancer", "Cryomancer", "Aquamancer", "Berserker", "Defender", "Revenant",
                "Avenger", "Crusader", "Protector", "Thunderlord", "Spiritguard", "Earthwarden",
                "Assassin", "Vindicator", "Apothecary", "Conjurer", "Sentinel", "Luminary"
            };

            foreach (var spec in specs)
            {
                var propertyName = $"{spec}Offset";
                var columnName = $"{spec.ToLowerInvariant()}_offset";
                entity.Property(typeof(int), propertyName).HasColumnName(columnName);
            }
        });
    }

    private static void ConfigureExperimentalSpecsWl(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecsWl>(entity =>
        {
            entity.ToTable("experimental_specs_wl");
            entity.HasKey(e => e.Uuid);
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.LastUpdated)
                .HasColumnName("last_updated")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
            ConfigureWlColumns(entity);
        });
    }

    private static void ConfigureExperimentalSpecsWlWeekly(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecsWlWeekly>(entity =>
        {
            entity.ToTable("experimental_specs_wl_weekly");
            entity.HasKey(e => new { e.Uuid, e.WeekStartDate });
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.WeekStartDate).HasColumnName("week_start_date");
            ConfigureWlColumns(entity);
        });
    }

    private static void ConfigureExperimentalSpecsWlDaily(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecsWlDaily>(entity =>
        {
            entity.ToTable("experimental_specs_wl_daily");
            entity.HasKey(e => new { e.Uuid, e.DayStartDate });
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.DayStartDate).HasColumnName("day_start_date");
            ConfigureWlColumns(entity);
        });
    }

    private static void ConfigureTimeWeek(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeWeek>(entity =>
        {
            entity.ToTable("time_week");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        });
    }

    private static void ConfigureTimeDay(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeDay>(entity =>
        {
            entity.ToTable("time_day");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        });
    }

    private static void ConfigureTimeSeason(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeSeason>(entity =>
        {
            entity.ToTable("time_season");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        });
    }

    private static void ConfigureSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.ToTable("settings");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value").HasColumnType("numeric");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
        });
    }

    private static void ConfigureCurrentWeekView(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecsWlCurrentWeek>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("experimental_specs_wl_current_week");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            ConfigureWlColumns(entity);
        });
    }

    private static void ConfigureCurrentDayView(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalSpecsWlCurrentDay>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("experimental_specs_wl_current_day");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            ConfigureWlColumns(entity);
        });
    }

    private static void ConfigureBalancePlayerDataView(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExperimentalBalancePlayerData>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("experimental_balance_player_data");
            entity.Property(e => e.Uuid).HasColumnName("uuid").HasColumnType("uuid");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.BaseWeight).HasColumnName("base_weight");
            entity.Property(e => e.DailyWinLoss).HasColumnName("daily_win_loss");
            entity.Property(e => e.GlobalNetKdPerGame)
                .HasColumnName("global_net_kd_per_game")
                .HasColumnType("double precision");
            ConfigureWeightColumns(entity);
            ConfigureWlColumns(entity);
        });
    }

    /// <summary>
    /// Shared column mapping for all WL stat entities (wins/losses/kills/deaths per spec).
    /// Uses dynamic type to work with any entity builder that has the same property names.
    /// </summary>
    private static void ConfigureWlColumns<T>(EntityTypeBuilder<T> entity)
        where T : class
    {
        var specs = new[]
        {
            "Pyromancer", "Cryomancer", "Aquamancer", "Berserker", "Defender", "Revenant",
            "Avenger", "Crusader", "Protector", "Thunderlord", "Spiritguard", "Earthwarden",
            "Assassin", "Vindicator", "Apothecary", "Conjurer", "Sentinel", "Luminary"
        };
        var stats = new[] { "Wins", "Losses", "Kills", "Deaths" };

        foreach (var spec in specs)
        {
            foreach (var stat in stats)
            {
                var propertyName = $"{spec}{stat}";
                var columnName = $"{spec.ToLowerInvariant()}_{stat.ToLowerInvariant()}";
                entity.Property(typeof(int), propertyName).HasColumnName(columnName);
            }
        }
    }

    private static void ConfigureWeightColumns<T>(EntityTypeBuilder<T> entity)
        where T : class
    {
        var specs = new[]
        {
            "Pyromancer", "Cryomancer", "Aquamancer", "Berserker", "Defender", "Revenant",
            "Avenger", "Crusader", "Protector", "Thunderlord", "Spiritguard", "Earthwarden",
            "Assassin", "Vindicator", "Apothecary", "Conjurer", "Sentinel", "Luminary"
        };

        foreach (var spec in specs)
        {
            var propertyName = $"{spec}Weight";
            var columnName = $"{spec.ToLowerInvariant()}_weight";
            entity.Property(typeof(int), propertyName).HasColumnName(columnName);
        }
    }
}