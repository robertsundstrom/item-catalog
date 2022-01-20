﻿using System;

using Microsoft.EntityFrameworkCore;

using Catalog.Domain.Entities;
using Catalog.Infrastructure;
using Catalog.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Catalog.Infrastructure.Persistence;

class CatalogContext : DbContext, ICatalogContext
{
    private readonly ICurrentUserService _currentUserService;

    public CatalogContext(DbContextOptions<CatalogContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Configurations.ItemConfiguration).Assembly);
    }

#nullable disable

    public DbSet<Item> Items { get; set; } = null!;

#nullable restore

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    entry.Entity.Created = DateTime.Now;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = _currentUserService.UserId;
                    entry.Entity.LastModified = DateTime.Now;
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        softDelete.DeletedBy = _currentUserService.UserId;
                        softDelete.Deleted = DateTime.Now;

                        entry.State = EntityState.Modified;
                    }
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<ITransaction> BeginTransactionAsync()
    {
        return new UoWTransaction(
            await Database.BeginTransactionAsync());
    }

    class UoWTransaction : ITransaction
    {
        private readonly IDbContextTransaction _transaction;

        public UoWTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync() => _transaction.CommitAsync();


        public void Dispose() => _transaction.Dispose();

        public Task RollbackAsync() => _transaction.RollbackAsync();
    }
}
