using Microsoft.EntityFrameworkCore;
using PurrVet.Migrations;
using PurrVet.Models;

namespace PurrVet.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentGroup> AppointmentGroups { get; set; }
        public DbSet<PetCard> PetCards { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<ServiceSubtype> ServiceSubtypes { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<MicrosoftAccountConnection> MicrosoftAccountConnections { get; set; }
        public DbSet<AppointmentDraft> AppointmentDrafts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ServiceCategory>()
                .HasMany(c => c.Subtypes)
                .WithOne(s => s.ServiceCategory)
                .HasForeignKey(s => s.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.ServiceCategory)
                .WithMany()
                .HasForeignKey(a => a.CategoryID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.ServiceSubtype)
                .WithMany()
                .HasForeignKey(a => a.SubtypeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
               .HasOne(a => a.AppointmentGroup)
               .WithMany(g => g.Appointments)
               .HasForeignKey(a => a.GroupID)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PetCard>()
                .HasOne(pc => pc.Appointment)
                .WithMany()
                .HasForeignKey(pc => pc.AppointmentID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
