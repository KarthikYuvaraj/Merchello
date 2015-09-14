﻿namespace Merchello.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    using Merchello.Core.EntityCollections;
    using Merchello.Core.Models;
    using Merchello.Core.Models.DetachedContent;
    using Merchello.Core.Models.Interfaces;
    using Merchello.Core.Services;

    using Newtonsoft.Json;

    using Umbraco.Core;
    using Umbraco.Core.Logging;

    using Formatting = Newtonsoft.Json.Formatting;

    /// <summary>
    /// The product extensions.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Reviewed. Suppression is OK here.")]
    public static class ProductExtensions
    {
        /// <summary>
        /// Returns a collection of ProductOption given as list of attributes (choices)
        /// </summary>
        /// <param name="product">
        /// The product
        /// </param>
        /// <param name="attributes">
        /// A collection of <see cref="IProductAttribute"/>
        /// </param>
        /// <remarks>
        /// This is mainly used for suggesting SKU defaults for ProductVariants
        /// </remarks>
        /// <returns>
        /// The collection of <see cref="IProductOption"/>.
        /// </returns>
        public static IEnumerable<IProductOption> ProductOptionsForAttributes(this IProduct product, IEnumerable<IProductAttribute> attributes)
        {
            var options = new List<IProductOption>();
            foreach (var att in attributes)
            {
                options.AddRange(product.ProductOptions.Where(option => option.Choices.Any(choice => choice.Key == att.Key)));
            }

            return options;
        }


        /// <summary>
        /// Returns the "master" <see cref="IProductVariant"/> that defines this <see cref="IProduct"/> or null if this <see cref="IProduct"/> has options
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <returns>
        /// <see cref="IProductVariant"/> or null if this <see cref="IProduct"/> has options
        /// </returns>
        public static IProductVariant GetProductVariantForPurchase(this IProduct product)
        {
            return product.ProductOptions.Any() ? null : ((Product)product).MasterVariant;
        }

        /// <summary>
        /// Returns the <see cref="IProductVariant"/> of this <see cref="IProduct"/> that contains a matching collection of <see cref="IProductAttribute"/>. 
        /// If not match is found, returns null.
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <param name="selectedChoices">
        /// A collection of <see cref="IProductAttribute"/> which define the specific <see cref="IProductVariant"/> of the <see cref="IProduct"/>
        /// </param>
        /// <returns>
        /// <see cref="IProductVariant"/> or null if no <see cref="IProductVariant"/> is found with a matching collection of <see cref="IProductAttribute"/>
        /// </returns>
        public static IProductVariant GetProductVariantForPurchase(this IProduct product, IEnumerable<IProductAttribute> selectedChoices)
        {
            return
                product.ProductVariants.FirstOrDefault(
                    variant =>
                    {
                        var productAttributes = selectedChoices as IProductAttribute[] ?? selectedChoices.ToArray();
                        return variant.Attributes.Count() == productAttributes.Count() &&
                                          productAttributes.All(item => ((ProductAttributeCollection)variant.Attributes).Contains(item.Key));
                    });
        }

        /// <summary>
        /// Returns the <see cref="IProductVariant"/> of this <see cref="IProduct"/> that contains a matching collection of <see cref="IProductAttribute"/>. 
        /// If not match is found, returns null.
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <param name="selectedChoiceKeys">
        /// The selected Choice Keys.
        /// </param>
        /// <returns>
        /// <see cref="IProductVariant"/> or null if no <see cref="IProductVariant"/> is found with a matching collection of <see cref="IProductAttribute"/>
        /// </returns>
        public static IProductVariant GetProductVariantForPurchase(this IProduct product, Guid[] selectedChoiceKeys)
        {
            return
                product.ProductVariants.FirstOrDefault(
                    variant => variant.Attributes.Count() == selectedChoiceKeys.Length &&
                               selectedChoiceKeys.All(key => ((ProductAttributeCollection)variant.Attributes).Contains(key)));
        }

        /// <summary>
        /// Associates a product with a warehouse catalog
        /// </summary>
        /// <param name="product">The <see cref="IProduct"/></param>
        /// <param name="catalog">The <see cref="IWarehouseCatalog"/></param>
        public static void AddToCatalogInventory(this IProduct product, IWarehouseCatalog catalog)
        {
            ((Product)product).MasterVariant.AddToCatalogInventory(catalog);
        }

        /// <summary>
        /// Associates a product variant with a warehouse
        /// </summary>
        /// <param name="productVariant">The <see cref="IProductVariant"/></param>
        /// <param name="catalog">The <see cref="IWarehouseCatalog"/></param>
        public static void AddToCatalogInventory(this IProductVariant productVariant, IWarehouseCatalog catalog)
        {
            productVariant.AddToCatalogInventory(catalog.Key);
        }

        /// <summary>
        /// Associates a product variant with a warehouse
        /// </summary>
        /// <param name="productVariant">
        /// The <see cref="IProductVariant"/>
        /// </param>
        /// <param name="catalogKey">
        /// The catalog Key.
        /// </param>
        public static void AddToCatalogInventory(this IProductVariant productVariant, Guid catalogKey)
        {
            productVariant.AddToCatalogInventory(new CatalogInventory(catalogKey, productVariant.Key));
        }

        /// <summary>
        /// The add to catalog inventory.
        /// </summary>
        /// <param name="productVariant">
        /// The <see cref="IProductVariant"/>
        /// </param>
        /// <param name="catalogInventory">
        /// The <see cref="ICatalogInventory"/> to be added
        /// </param>
        public static void AddToCatalogInventory(this IProductVariant productVariant, ICatalogInventory catalogInventory)
        {
            ((CatalogInventoryCollection)productVariant.CatalogInventories).Add(catalogInventory);
        }

        /// <summary>
        /// Removes a product variant from a catalog inventory.
        /// </summary>
        /// <param name="productVariant">
        /// The product variant.
        /// </param>
        /// <param name="catalog">
        /// The catalog.
        /// </param>
        public static void RemoveFromCatalogInventory(this IProductVariant productVariant, IWarehouseCatalog catalog)
        {
            if (productVariant.CatalogInventories.All(inv => inv.CatalogKey != catalog.Key)) return;

            productVariant.RemoveFromCatalogInventory(catalog.Key);
        }

        /// <summary>
        /// Removes a product variant from a catalog inventory.
        /// </summary>
        /// <param name="productVariant">
        /// The product variant.
        /// </param>
        /// <param name="catalogKey">
        /// The catalog key
        /// </param>
        public static void RemoveFromCatalogInventory(this IProductVariant productVariant, Guid catalogKey)
        {
            if (productVariant.CatalogInventories.All(inv => inv.CatalogKey != catalogKey)) return;
            ((CatalogInventoryCollection)productVariant.CatalogInventories).RemoveAt(productVariant.CatalogInventories.FindIndex(x => x.CatalogKey == catalogKey));
        }

        /// <summary>
        /// The get possible product attribute combinations.
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <returns>
        /// The collection of product attribute collections.
        /// </returns>
        internal static IEnumerable<IEnumerable<IProductAttribute>> GetPossibleProductAttributeCombinations(this IProduct product)
        {
            if (!product.ProductOptions.Any()) return new List<IEnumerable<IProductAttribute>>();

            var optionChoices = product.ProductOptions.Select(option => option.Choices.ToList()).ToList();

            return optionChoices.CartesianProduct();
        }

        #region Static Product Collections

        /// <summary>
        /// The add to collection.
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <param name="collection">
        /// The collection.
        /// </param>
        public static void AddToCollection(this IProduct product, IEntityCollection collection)
        {
            product.AddToCollection(collection.Key);
        }

        /// <summary>
        /// Adds a product to a static product collection.
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <param name="collectionKey">
        /// The collection key.
        /// </param>
        public static void AddToCollection(this IProduct product, Guid collectionKey)
        {
            if (!EntityCollectionProviderResolver.HasCurrent || !MerchelloContext.HasCurrent) return;
            var attempt = EntityCollectionProviderResolver.Current.GetProviderForCollection(collectionKey);
            if (!attempt.Success) return;

            var provider = attempt.Result;

            if (!provider.EnsureEntityType(EntityType.Product))
            {
                LogHelper.Debug(typeof(ProductExtensions), "Attempted to add a product to a non product collection");
                return;
            }

            MerchelloContext.Current.Services.ProductService.AddToCollection(product.Key, collectionKey);
        }

        /// <summary>
        /// Removes a product from a collection.
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <param name="collection">
        /// The collection.
        /// </param>
        public static void RemoveFromCollection(this IProduct product, IEntityCollection collection)
        {
            product.RemoveFromCollection(collection.Key);
        }

        /// <summary>
        /// Removes a product from a collection.
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <param name="collectionKey">
        /// The collection key.
        /// </param>        
        public static void RemoveFromCollection(this IProduct product, Guid collectionKey)
        {
            if (!MerchelloContext.HasCurrent) return;
            MerchelloContext.Current.Services.ProductService.RemoveFromCollection(product.Key, collectionKey);
        }

        /// <summary>
        /// Returns static collections containing the product.
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        /// <remarks>
        /// This is internal so that people do not query for these entries in a big product list 
        /// which would be really excessive database calls.
        /// TODO need to decide how to cache these to provide that functionality
        /// </remarks>
        internal static IEnumerable<IEntityCollection> GetCollectionsContaining(this IProduct product)
        {
            if (!MerchelloContext.HasCurrent) return Enumerable.Empty<IEntityCollection>();
            return
                ((EntityCollectionService)MerchelloContext.Current.Services.EntityCollectionService)
                    .GetEntityCollectionsByProductKey(product.Key);
        }

        #endregion



        #region ProductAttributeCollection

        /// <summary>
        /// Converts an enumeration of ProductAttributes to a ProductAttributeCollection
        /// </summary>
        /// <param name="attributes">
        /// The attributes.
        /// </param>
        /// <returns>
        /// The <see cref="ProductAttributeCollection"/>.
        /// </returns>
        internal static ProductAttributeCollection ToProductAttributeCollection(this IEnumerable<IProductAttribute> attributes)
        {
            var collection = new ProductAttributeCollection();
            foreach (var att in attributes)
            {
                collection.Add(att);
            }

            return collection;
        }

        #endregion


        #region Examine Serialization

        /// <summary>
        /// Serializes <see cref="IProduct"/> object's variants
        /// </summary>
        /// <param name="product">
        /// The product.
        /// </param>
        /// <remarks>
        /// Intended to be used by the Merchello.Examine.Providers.MerchelloProductIndexer
        /// </remarks>
        /// <returns>
        /// The <see cref="XDocument"/>.
        /// </returns>
        public static XDocument SerializeToXml(this IProduct product)
        {
            string xml;
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("product");
                    writer.WriteAttributeString("key", product.Key.ToString());                                        
                    writer.WriteEndElement(); // product
                    writer.WriteEndDocument();
                    xml = sw.ToString();
                }
            }

            var doc = XDocument.Parse(xml);
            if (doc.Root == null) return XDocument.Parse("<product />");
                
            doc.Root.Add(((Product)product).MasterVariant.SerializeToXml(product.ProductOptions, product.GetCollectionsContaining().Select(x => x.Key)).Root);

            // Need to filter out the Master variant so that it does not get overwritten in the cases where
            // a product defines options.
            // http://issues.merchello.com/youtrack/issue/M-152
            foreach (var variant in product.ProductVariants.Where(x => ((ProductVariant)x).Master == false))
            {
                doc.Root.Add(variant.SerializeToXml().Root);
            }
            return doc;
        }

        /// <summary>
        /// Serializes a product variant for Examine indexing.
        /// </summary>
        /// <param name="productVariant">
        /// The product variant.
        /// </param>
        /// <param name="productOptionCollection">
        /// The product option collection.
        /// </param>
        /// <param name="collections">
        /// Static collections keys product belongs 
        /// </param>
        /// <returns>
        /// The <see cref="XDocument"/>.
        /// </returns>
        internal static XDocument SerializeToXml(this IProductVariant productVariant, ProductOptionCollection productOptionCollection = null, IEnumerable<Guid> collections = null)
        {
            string xml;
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("productVariant");
                    writer.WriteAttributeString("id", ((ProductVariant)productVariant).ExamineId.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("productKey", productVariant.ProductKey.ToString());
                    writer.WriteAttributeString("productVariantKey", productVariant.Key.ToString());
                    writer.WriteAttributeString("master", ((ProductVariant)productVariant).Master.ToString());
                    writer.WriteAttributeString("name", productVariant.Name);
                    writer.WriteAttributeString("sku", productVariant.Sku);
                    writer.WriteAttributeString("price", productVariant.Price.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("manufacturer", productVariant.Manufacturer);
                    writer.WriteAttributeString("modelNumber", productVariant.ManufacturerModelNumber);
                    writer.WriteAttributeString("costOfGoods", productVariant.CostOfGoods.ToString());
                    writer.WriteAttributeString("salePrice", productVariant.SalePrice.ToString());
                    writer.WriteAttributeString("onSale", productVariant.OnSale.ToString());
                    writer.WriteAttributeString("weight", productVariant.Weight.ToString());
                    writer.WriteAttributeString("length", productVariant.Length.ToString());
                    writer.WriteAttributeString("width", productVariant.Width.ToString());
                    writer.WriteAttributeString("height", productVariant.Height.ToString());
                    writer.WriteAttributeString("barcode", productVariant.Barcode);
                    writer.WriteAttributeString("available", productVariant.Available.ToString());
                    writer.WriteAttributeString("trackInventory", productVariant.TrackInventory.ToString());
                    writer.WriteAttributeString("outOfStockPurchase", productVariant.OutOfStockPurchase.ToString());
                    writer.WriteAttributeString("taxable", productVariant.Taxable.ToString());
                    writer.WriteAttributeString("shippable", productVariant.Shippable.ToString());
                    writer.WriteAttributeString("download", productVariant.Download.ToString());
                    writer.WriteAttributeString("downloadMediaId", productVariant.DownloadMediaId.ToString());
                    writer.WriteAttributeString("totalInventoryCount", productVariant.TotalInventoryCount.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("attributes", GetAttributesJson(productVariant));
                    writer.WriteAttributeString("catalogInventories", GetCatalogInventoriesJson(productVariant));
                    writer.WriteAttributeString("productOptions", GetProductOptionsJson(productOptionCollection));
                    writer.WriteAttributeString("slugs", string.Join(" ", productVariant.DetachedContents.Select(x => x.Slug)));
                    writer.WriteAttributeString("detachedContents", GetDetachedContentsJson(((ProductVariant)productVariant).DetachedContents));        
                    writer.WriteAttributeString("versionKey", productVariant.VersionKey.ToString());

                    // 1.11.0 - static collections
                    if (collections != null)
                    {
                        var collectionKeys = collections as Guid[] ?? collections.ToArray();
                        if (collectionKeys.Any())
                        {
                            writer.WriteAttributeString("staticCollectionKeys", string.Join(" ", collectionKeys));
                        }
                    }
                  
                    writer.WriteAttributeString("createDate", productVariant.CreateDate.ToString("s"));
                    writer.WriteAttributeString("updateDate", productVariant.UpdateDate.ToString("s"));                    
                    writer.WriteAttributeString("allDocs", "1");
                                        
                    writer.WriteEndElement(); // product variant
                    writer.WriteEndDocument();

                    xml = sw.ToString();
                }
            }

            return XDocument.Parse(xml); 
        }

        /// <summary>
        /// Converts the product options collections to JSON.
        /// </summary>
        /// <param name="productOptionCollection">
        /// The product option collection.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        internal static string ToJsonProductOptions(this ProductOptionCollection productOptionCollection)
        {
            return GetProductOptionsJson(productOptionCollection);
        }

        /// <summary>
        /// Gets product options JSON.
        /// </summary>
        /// <param name="productOptions">
        /// The product options.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetProductOptionsJson(IEnumerable<IProductOption> productOptions)
        {
            var json = "[{0}]";
            var options = string.Empty;

            if (productOptions != null)
            {
                foreach (var option in productOptions)
                {
                    var optionChoices = new List<object>();                
                    foreach (var choice in option.Choices)
                    {
                        optionChoices.Add(
                                new
                                {
                                    choice.Key,
                                    optionKey = choice.OptionKey,
                                    name = choice.Name,
                                    sortOrder = choice.SortOrder
                                });
                    }

                    if (options.Length > 0) options += ",";
                    options += JsonConvert.SerializeObject(
                            new
                            {
                                option.Key,
                                name = option.Name,
                                required = option.Required,
                                sortOrder = option.SortOrder,
                                choices = optionChoices
                            });
                }
            }

            json = string.Format(json, options);

            return json;
        }

        /// <summary>
        /// Gets detached detachedContentCollection JSON.
        /// </summary>
        /// <param name="detachedContentCollection">
        /// The detachedContentCollection.
        /// </param>
        /// <returns>
        /// The JSON string.
        /// </returns>
        private static string GetDetachedContentsJson(IEnumerable<IProductVariantDetachedContent> detachedContentCollection)
        {
            const string Json = "{0}";
            var contents = string.Empty;

            if (detachedContentCollection != null)
            {
                var generic = new List<object>();
                foreach (var content in detachedContentCollection)
                {
                    generic.Add(
                        new 
                        {
                            content.Key,
                            DetachedContentType = content.DetachedContentType as DetachedContentType,
                            content.CultureName,
                            content.TemplateId,
                            content.ProductVariantKey,
                            content.Slug,
                            content.CanBeRendered,
                            DetachedDataValues = content.DetachedDataValues.AsEnumerable(),
                            content.CreateDate,
                            content.UpdateDate
                        });
                }

                if (generic.Any()) contents = JsonConvert.SerializeObject(generic.ToArray(), Formatting.None);
            }

            return string.Format(Json, contents);
        }

        /// <summary>
        /// Gets the catalog inventories JSON.
        /// </summary>
        /// <param name="productVariant">
        /// The product variant.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetCatalogInventoriesJson(IProductVariant productVariant)
        {
            var json = "[{0}]";
            var catalogInventories = string.Empty;

            foreach (var ch in productVariant.CatalogInventories)
            {
                if (catalogInventories.Length > 0) catalogInventories += ",";
                catalogInventories += JsonConvert.SerializeObject(
                new
                {   
                    catalogKey = ch.CatalogKey,
                    productVariantKey = ch.ProductVariantKey,
                    location = ch.Location,
                    count = ch.Count,
                    lowCount = ch.LowCount
                },
                Formatting.None);
            }

            json = string.Format(json, catalogInventories);
            return json;
        }

        /// <summary>
        /// Gets the attributes JSON.
        /// </summary>
        /// <param name="productVariant">
        /// The product variant.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetAttributesJson(IProductVariant productVariant)
        {
            var json = "[{0}]";
            var atts = string.Empty;

            foreach (var attribute in productVariant.Attributes)
            {
                if (atts.Length > 0) atts += ",";
                atts += JsonConvert.SerializeObject(
                new 
                { 
                    key = attribute.Key,
                    optionKey = attribute.OptionKey,
                    name = attribute.Name,
                    sku = attribute.Sku,
                    sortOrder = attribute.SortOrder                    
                }, 
                Formatting.None);
            }
            json = string.Format(json, atts);
            return json;
        }

        #endregion
    }
}