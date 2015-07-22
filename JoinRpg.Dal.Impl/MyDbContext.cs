﻿using System;
using System.Data.Entity;
using System.Net.Http.Headers;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl
{
  public class MyDbContext : DbContext, IUnitOfWork
  {
    public MyDbContext() : base("DefaultConnection")
    { }

    public DbSet<Project> ProjectsSet => Set<Project>();

    public DbSet<User> UserSet => Set<User>();

    DbSet<T> IUnitOfWork.GetDbSet<T>() => Set<T>();

    void IUnitOfWork.SaveChanges() => SaveChanges();

    public static MyDbContext Create()
    {
      return new MyDbContext();
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Entity<ProjectAcl>().
        HasKey(c => new {c.UserId, c.ProjectId});

      modelBuilder.Entity<Project>().HasRequired(p => p.CreatorUser).WithMany().HasForeignKey(p => p.CreatorUserId).WillCascadeOnDelete(false);

      modelBuilder.Entity<CharacterGroup>().HasMany(cg => cg.ParentGroups).WithMany(cg => cg.ChildGroups);

      modelBuilder.Entity<Character>().HasMany(c => c.Groups).WithMany(cg => cg.Characters);

      modelBuilder.Entity<Project>().HasMany(p => p.Characters).WithRequired(c => c.Project).WillCascadeOnDelete(false);

      modelBuilder.Entity<CharacterGroup>().HasMany(cg =>cg.Claims).WithOptional(c => c.Group).WillCascadeOnDelete(false);
      modelBuilder.Entity<Character>().HasMany(cg => cg.Claims).WithOptional(c => c.Character).WillCascadeOnDelete(false);
      modelBuilder.Entity<Claim>().HasRequired(c => c.Player). WithMany(p => p.Claims).WillCascadeOnDelete(false);
      modelBuilder.Entity<Claim>().HasRequired(c => c.Project).WithMany().WillCascadeOnDelete(false);

      base.OnModelCreating(modelBuilder);
    }
 }
}
