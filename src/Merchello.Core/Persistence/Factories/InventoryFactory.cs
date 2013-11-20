﻿using Merchello.Core.Models;
using Merchello.Core.Models.Rdbms;

namespace Merchello.Core.Persistence.Factories
{
    internal class InventoryFactory : IEntityFactory<IWarehouseInventory, WarehouseInventoryDto>
    {
        public IWarehouseInventory BuildEntity(WarehouseInventoryDto dto)
        {
            return new WarehouseInventory(dto.WarehouseKey, dto.ProductVariantKey)
                {
                    Count = dto.Count,
                    LowCount = dto.LowCount,
                    CreateDate = dto.CreateDate,
                    UpdateDate = dto.UpdateDate
                };
        }

        public WarehouseInventoryDto BuildDto(IWarehouseInventory entity)
        {
            return new WarehouseInventoryDto()
                {
                    WarehouseKey = entity.WarehouseKey,
                    ProductVariantKey = entity.ProductVariantKey,
                    Count = entity.Count,
                    LowCount = entity.LowCount,
                    CreateDate = entity.CreateDate,
                    UpdateDate = entity.UpdateDate
                };
        }
    }
}