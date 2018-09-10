﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Onboarding.Models;

namespace Onboarding.Migrations
{
    [DbContext(typeof(OnboardingContext))]
    partial class OnboardingContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.2-rtm-30932")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Onboarding.Models.Channel", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ChannelName");

                    b.Property<string>("WorkspaceId");

                    b.HasKey("Id");

                    b.HasIndex("WorkspaceId");

                    b.ToTable("Channel");
                });

            modelBuilder.Entity("Onboarding.Models.UserAccount", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("EmailId");

                    b.Property<string>("FirstName");

                    b.Property<bool>("IsVerified");

                    b.Property<string>("LastName");

                    b.Property<string>("Password");

                    b.HasKey("Id");

                    b.ToTable("UserAccount");
                });

            modelBuilder.Entity("Onboarding.Models.UserState", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("EmailId");

                    b.Property<bool>("IsJoined");

                    b.Property<string>("Otp");

                    b.Property<string>("WorkspaceId");

                    b.HasKey("Id");

                    b.HasIndex("WorkspaceId");

                    b.ToTable("UserState");
                });

            modelBuilder.Entity("Onboarding.Models.UserWorkspace", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("WorkspaceId");

                    b.HasKey("UserId", "WorkspaceId");

                    b.HasIndex("WorkspaceId");

                    b.ToTable("UserWorkspaces");
                });

            modelBuilder.Entity("Onboarding.Models.Workspace", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("PictureUrl");

                    b.Property<string>("WorkspaceName");

                    b.HasKey("Id");

                    b.ToTable("Workspace");
                });

            modelBuilder.Entity("Onboarding.Models.WorkspaceName", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<string>("UserAccountId");

                    b.HasKey("Id");

                    b.HasIndex("UserAccountId");

                    b.ToTable("WorkspaceName");
                });

            modelBuilder.Entity("Onboarding.Models.Channel", b =>
                {
                    b.HasOne("Onboarding.Models.Workspace")
                        .WithMany("Channels")
                        .HasForeignKey("WorkspaceId");
                });

            modelBuilder.Entity("Onboarding.Models.UserState", b =>
                {
                    b.HasOne("Onboarding.Models.Workspace")
                        .WithMany("UsersState")
                        .HasForeignKey("WorkspaceId");
                });

            modelBuilder.Entity("Onboarding.Models.UserWorkspace", b =>
                {
                    b.HasOne("Onboarding.Models.UserAccount", "UserAccount")
                        .WithMany("UserWorkspaces")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Onboarding.Models.Workspace", "Workspace")
                        .WithMany("UserWorkspaces")
                        .HasForeignKey("WorkspaceId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Onboarding.Models.WorkspaceName", b =>
                {
                    b.HasOne("Onboarding.Models.UserAccount")
                        .WithMany("Workspaces")
                        .HasForeignKey("UserAccountId");
                });
#pragma warning restore 612, 618
        }
    }
}
