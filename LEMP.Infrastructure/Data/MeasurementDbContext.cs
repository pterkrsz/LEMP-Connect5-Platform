using LEMP.Domain;
using Microsoft.EntityFrameworkCore;

namespace LEMP.Infrastructure.Data;

public class MeasurementDbContext : DbContext
{
    public MeasurementDbContext(DbContextOptions<MeasurementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Measurement> Measurements => Set<Measurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Measurement>().Ignore(m => m.Values);
        base.OnModelCreating(modelBuilder);
    }
}
