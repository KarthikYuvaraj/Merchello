﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Merchello.Core.Gateways.Shipping.FixedRate;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Merchello.Core;
using Merchello.Core.Models;
using Merchello.Core.Services;
using Merchello.Web.WebApi;
using Merchello.Web.Models.ContentEditing;
using System.Net;
using System.Net.Http;
using Merchello.Core.Gateways.Shipping;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Merchello.Web.Editors
{
    [PluginController("Merchello")]
    public class CatalogRateTableShippingApiController : MerchelloApiController
    {
        private readonly IShipCountryService _shipCountryService;
        private readonly FixedRateShippingGatewayProvider _fixedRateShippingGatewayProvider;
        private readonly IShippingContext _shippingContext;

        /// <summary>
        /// Constructor
        /// </summary>
        public CatalogRateTableShippingApiController()
            : this(MerchelloContext.Current)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="merchelloContext"></param>
        public CatalogRateTableShippingApiController(MerchelloContext merchelloContext)
            : base(merchelloContext)
        {
            _shipCountryService = ((ServiceContext) MerchelloContext.Services).ShipCountryService;
            _fixedRateShippingGatewayProvider = (FixedRateShippingGatewayProvider)MerchelloContext.Gateways.Shipping.ResolveByKey(Constants.ProviderKeys.Shipping.FixedRateShippingProviderKey);
            _shippingContext = MerchelloContext.Gateways.Shipping;
        }

        /// <summary>
        /// This is a helper contructor for unit testing
        /// </summary>
        internal CatalogRateTableShippingApiController(MerchelloContext merchelloContext, UmbracoContext umbracoContext)
            : base(merchelloContext, umbracoContext)
        {
            _shipCountryService = ((ServiceContext)MerchelloContext.Services).ShipCountryService;
            _fixedRateShippingGatewayProvider = (FixedRateShippingGatewayProvider)MerchelloContext.Gateways.Shipping.ResolveByKey(Constants.ProviderKeys.Shipping.FixedRateShippingProviderKey);
        }

        /// <summary>
        /// 
        ///
        /// GET /umbraco/Merchello/ShippingMethodsApi/GetAllShipCountryFixedRateProviders/{id}
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="id">ShipCountry Key</param>
        public IEnumerable<ShippingGatewayProviderDisplay> GetAllShipCountryFixedRateProviders(Guid id)
        {
            var shipCountry = _shipCountryService.GetByKey(id);
            if (shipCountry != null)
            {
                var providers = _shippingContext.GetGatewayProvidersByShipCountry(shipCountry);

                var fixedProviders = providers.Where(x => x.Key == Constants.ProviderKeys.Shipping.FixedRateShippingProviderKey);

                foreach (IShippingGatewayProvider provider in fixedProviders)
                {
                    yield return provider.ToShipGatewayProviderDisplay();
                }
            }
            else
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            }
        }

        /// <summary>
        /// 
        ///
        /// GET /umbraco/Merchello/ShippingMethodsApi/GetAllFixedRateProviderMethods/{id}
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="id">ShipCountry Key</param>
        public IEnumerable<RateTableShipMethodDisplay> GetAllFixedRateProviderMethods(Guid id)
        {
            var shipCountry = _shipCountryService.GetByKey(id);
            if (shipCountry != null)
            {
                var providers = _shippingContext.GetGatewayProvidersByShipCountry(shipCountry);

                if (providers.Count() > 0)
                {
                    var fixedProvider = providers.FirstOrDefault(x => x.Key == Constants.ProviderKeys.Shipping.FixedRateShippingProviderKey);

                    if (fixedProvider != null)
                    {
                        foreach (IShippingGatewayMethod method in fixedProvider.GetAllShippingGatewayMethods(shipCountry))
                        {
                            IFixedRateShippingGatewayMethod fixedRateShippingGatewayMethod = method as IFixedRateShippingGatewayMethod;
                            yield return fixedRateShippingGatewayMethod.ToRateTableShipMethodDisplay();
                        }
                    }
                    else
                    {
                        throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
                    }
                }
                else
                {
                    throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
                }
            }
            else
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            }
        }

        /// <summary>
        /// Add a Fixed Rate Table ShipMethod to the ShipCountry
        ///
        /// GET /umbraco/Merchello/ShippingMethodsApi/AddRateTableShipMethod
        /// </summary>
        /// <param name="method">POSTed RateTableShipMethodDisplay object</param>
        [AcceptVerbs("POST")]
        public HttpResponseMessage AddRateTableShipMethod(RateTableShipMethodDisplay method)
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);

            try
            {
                var shipCountry = _shipCountryService.GetByKey(method.ShipMethod.ShipCountryKey);
                var provider = _fixedRateShippingGatewayProvider;

                var merchelloGwShipMethod = (IFixedRateShippingGatewayMethod)provider.CreateShipMethod(method.RateTableType, shipCountry, method.ShipMethod.Name);

                merchelloGwShipMethod = method.ToRateTableShipMethod(merchelloGwShipMethod);

                provider.SaveShippingGatewayMethod(merchelloGwShipMethod);
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, String.Format("{0}", ex.Message));
            }

            return response;
        }

        /// <summary>
        /// Save a Fixed Rate Table ShipMethod to the ShipCountry
        /// 
        /// GET /umbraco/Merchello/ShippingMethodsApi/PutRateTableShipMethod
        /// </summary>
        /// <param name="method">POSTed ShipMethodDisplay object</param>
        [AcceptVerbs("POST", "PUT")]
        public HttpResponseMessage PutRateTableShipMethod(RateTableShipMethodDisplay method)
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);

            try
            {
                var shipCountry = _shipCountryService.GetByKey(method.ShipMethod.ShipCountryKey);
                var provider = _fixedRateShippingGatewayProvider;

                var merchelloMethod = (IFixedRateShippingGatewayMethod)provider.GetAllShippingGatewayMethods(shipCountry).FirstOrDefault(m => m.ShipMethod.Key == method.ShipMethod.Key);

                if (merchelloMethod != null)
                {
                    merchelloMethod = method.ToRateTableShipMethod(merchelloMethod);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }

                provider.SaveShippingGatewayMethod(merchelloMethod);
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, String.Format("{0}", ex.Message));
            }

            return response;
        }

        /// <summary>
        /// Save an external ShipMethod to the ShipCountry
        /// 
        /// GET /umbraco/Merchello/ShippingMethodsApi/DeleteRateTableShipMethod
        /// </summary>
        /// <param name="method">POSTed ShipMethodDisplay object</param>
        [AcceptVerbs("POST", "PUT")]
        public ShipMethodDisplay DeleteRateTableShipMethod(ShipMethodDisplay method)
        {
            return null;
        }
    }
}