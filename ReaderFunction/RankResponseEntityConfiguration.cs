using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReaderFunction.Models;

namespace ReaderFunction
{
    public class RankResponseEntityConfiguration : IEntityTypeConfiguration<RankResponseModel>
    {
        public void Configure(EntityTypeBuilder<RankResponseModel> builder)
        {

            builder.ToTable("company")
                .HasKey(x => x.Symbol);

        }
    }
}

