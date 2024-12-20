﻿// The Group17_iCAREDBEntities1 class inherits from Entity Framework's DbContext and is responsible for interacting with the database.
// This class manages the entities that map to database tables through several DbSet properties.
// Each DbSet property handles data for a related entity class (DocumentMetadata, DrugsDictionary, iCAREUser, etc.).

namespace Group17_iCAREAPP.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class Group17_iCAREDBEntities1 : DbContext
    {
        public Group17_iCAREDBEntities1()
            : base("name=Group17_iCAREDBEntities1")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<DocumentMetadata> DocumentMetadata { get; set; }
        public virtual DbSet<DrugsDictionary> DrugsDictionary { get; set; }
        public virtual DbSet<GeoCodes> GeoCodes { get; set; }
        public virtual DbSet<iCAREAdmin> iCAREAdmin { get; set; }
        public virtual DbSet<iCAREUser> iCAREUser { get; set; }
        public virtual DbSet<iCAREWorker> iCAREWorker { get; set; }
        public virtual DbSet<ModificationHistory> ModificationHistory { get; set; }
        public virtual DbSet<PatientRecord> PatientRecord { get; set; }
        public virtual DbSet<TreatmentRecord> TreatmentRecord { get; set; }
        public virtual DbSet<UserPassword> UserPassword { get; set; }
        public virtual DbSet<UserRole> UserRole { get; set; }
    }
}
