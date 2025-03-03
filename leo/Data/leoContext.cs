using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using leo.Models;

namespace leo.Data
{
    public class leoContext : DbContext
    {
        public leoContext (DbContextOptions<leoContext> options)
            : base(options)
        {
        }

        public DbSet<leo.Models.Users> Users { get; set; } = default!;

        public DbSet<leo.Models.Role>? Role { get; set; }

        public DbSet<leo.Models.Category>? Category { get; set; }

        public DbSet<leo.Models.Inventory>? Inventory { get; set; }

        public DbSet<leo.Models.Supplier>? Supplier { get; set; }

        public DbSet<leo.Models.Order>? Order { get; set; }

        public DbSet<AuditLog>? AuditLogs { get; set; }

        public DbSet<leo.Models.Return>? Return { get; set; }

        //public DbSet<leo.Models.StockAlert>? StockAlert { get; set; }

           public DbSet<leo.Models.TransactionHistory>? TransactionHistory { get; set; }



        //public DbSet<leo.Models.StockAlert>? StockAlert { get; set; }




        public DbSet<leo.Models.SupplierProfile>? SupplierProfile { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Prevent deletion of products that have associated orders
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deletion of products with associated sales

            base.OnModelCreating(modelBuilder);



        }




        //public DbSet<leo.Models.StockAlert>? StockAlert { get; set; }


    

  

     


     





    }

}
