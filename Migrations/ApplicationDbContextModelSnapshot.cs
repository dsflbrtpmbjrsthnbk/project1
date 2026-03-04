using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using UserManagementApp.Data;

#nullable disable

namespace UserManagementApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("UserManagementApp.Models.User", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("EmailVerificationToken")
                    .HasColumnType("text");

                b.Property<DateTime?>("LastLoginTime")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("PasswordHash")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<DateTime>("RegistrationTime")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW()");

                b.Property<string>("Status")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnType("text")
                    .HasDefaultValue("unverified");

                b.HasKey("Id");

                b.HasIndex("Email")
                    .IsUnique()
                    .HasDatabaseName("UX_Users_Email");

                b.HasIndex("LastLoginTime")
                    .HasDatabaseName("IX_Users_LastLoginTime");

                b.ToTable("Users");
            });
#pragma warning restore 612, 618
        }
    }
}
