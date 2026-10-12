namespace CashFlow.DataAccess
{
    using CashFlow.DataAccess.Entities;
    using log4net;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;

    public class Context : DbContext
    {
        private ILog Log = LogManager.GetLogger("Context:");
        public Context(DbContextOptions<Context> options) : base(options)
        {
            this.downloadRunning = false;
        }

        public DbSet<AccountEntity> accounts { get; set; } = null!;
        public DbSet<AccountPayableEntity> accountspayable { get; set; } = null!;
        public DbSet<AccountReceivableEntity> accountsreceivable { get; set; } = null!;
        public DbSet<ExpenseTypeEntity> expenses { get; set; } = null!;
        public DbSet<RevenueTypeEntity> revenues { get; set; } = null!;
        public DbSet<FixedExpenseEntity> fixedexpenses { get; set; } = null!;
        public DbSet<UserEntity> users { get; set; } = null!;

        private bool downloadRunning = false;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (downloadRunning)
            {
                Log.Info("Download is running. Auto-Increment, from all tables, is being disabled.");

                modelBuilder.Entity<AccountEntity>()
                    .Property(b => b.OID)
                    .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

                modelBuilder.Entity<AccountEntity>().Property(b => b.OID).HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
                modelBuilder.Entity<AccountPayableEntity>().Property(b => b.OID).HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
                modelBuilder.Entity<AccountReceivableEntity>().Property(b => b.OID).HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
                modelBuilder.Entity<ExpenseTypeEntity>().Property(b => b.OID).HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
                modelBuilder.Entity<RevenueTypeEntity>().Property(b => b.OID).HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
                modelBuilder.Entity<FixedExpenseEntity>().Property(b => b.OID).HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
                modelBuilder.Entity<UserEntity>().Property(b => b.OID).HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
