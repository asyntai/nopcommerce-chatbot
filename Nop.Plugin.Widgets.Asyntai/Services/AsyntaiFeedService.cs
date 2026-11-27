using System.Security.Cryptography;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Html;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Topics;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Widgets.Asyntai.Services;

/// <summary>
/// The signed, read-only feed Asyntai reads the store's products and pages
/// from (Asyntai store feed v1).
/// </summary>
/// <remarks>
/// Everything is read as the built-in search engine customer, a guest, so the
/// feed holds only what an anonymous visitor can see: published products
/// that are visible on their own, in this store, without customer role limits.
/// The work context is never touched, because that would create a new guest
/// customer for every feed request.
/// </remarks>
public class AsyntaiFeedService
{
    #region Fields

    protected readonly CurrencySettings _currencySettings;
    protected readonly IAclService _aclService;
    protected readonly ICategoryService _categoryService;
    protected readonly ICurrencyService _currencyService;
    protected readonly ICustomerService _customerService;
    protected readonly IHtmlFormatter _htmlFormatter;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IManufacturerService _manufacturerService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPermissionService _permissionService;
    protected readonly IPictureService _pictureService;
    protected readonly IPriceCalculationService _priceCalculationService;
    protected readonly IProductService _productService;
    protected readonly IRepository<Category> _categoryRepository;
    protected readonly IRepository<Manufacturer> _manufacturerRepository;
    protected readonly IRepository<Product> _productRepository;
    protected readonly ISettingService _settingService;
    protected readonly IStoreMappingService _storeMappingService;
    protected readonly ITaxService _taxService;
    protected readonly ITopicService _topicService;

    #endregion

    #region Ctor

    public AsyntaiFeedService(CurrencySettings currencySettings,
        IAclService aclService,
        ICategoryService categoryService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IHtmlFormatter htmlFormatter,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IManufacturerService manufacturerService,
        INopUrlHelper nopUrlHelper,
        IPermissionService permissionService,
        IPictureService pictureService,
        IPriceCalculationService priceCalculationService,
        IProductService productService,
        IRepository<Category> categoryRepository,
        IRepository<Manufacturer> manufacturerRepository,
        IRepository<Product> productRepository,
        ISettingService settingService,
        IStoreMappingService storeMappingService,
        ITaxService taxService,
        ITopicService topicService)
    {
        _currencySettings = currencySettings;
        _aclService = aclService;
        _categoryService = categoryService;
        _currencyService = currencyService;
        _customerService = customerService;
        _htmlFormatter = htmlFormatter;
        _languageService = languageService;
        _localizationService = localizationService;
        _manufacturerService = manufacturerService;
        _nopUrlHelper = nopUrlHelper;
        _permissionService = permissionService;
        _pictureService = pictureService;
        _priceCalculationService = priceCalculationService;
        _productService = productService;
        _categoryRepository = categoryRepository;
        _manufacturerRepository = manufacturerRepository;
        _productRepository = productRepository;
        _settingService = settingService;
        _storeMappingService = storeMappingService;
        _taxService = taxService;
        _topicService = topicService;
    }

    #endregion

    #region Signature

    /// <summary>
    /// The tokens a feed request may be signed with: the live one while
    /// sharing is on, and the pending one while a connect is in progress,
    /// because Asyntai starts reading the moment the owner signs in.
    /// </summary>
    public static IList<string> AcceptedTokens(AsyntaiSettings settings)
    {
        var tokens = new List<string>();
        if (settings.FeedEnabled && !string.IsNullOrEmpty(settings.FeedToken))
            tokens.Add(settings.FeedToken);

        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - settings.ConnectStateAt;
        if (!string.IsNullOrEmpty(settings.PendingToken) && age >= 0 && age <= AsyntaiDefaults.ConnectStateTtl)
            tokens.Add(settings.PendingToken);

        return tokens;
    }

    /// <summary>
    /// An error text when the request is not a valid signed call, else null.
    /// Signed over the raw query values, in a fixed order, before clamping.
    /// </summary>
    public virtual async Task<string> RefuseAsync(string page, string limit, string ids, string ts, string sig)
    {
        var settings = await _settingService.LoadSettingAsync<AsyntaiSettings>();
        var tokens = AcceptedTokens(settings);
        if (tokens.Count == 0)
            return "Sharing is switched off on this site.";

        ts ??= string.Empty;
        if (ts.Length == 0 || ts.Length > 12 || !ts.All(char.IsAsciiDigit))
            return "Missing timestamp.";

        if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - long.Parse(ts)) > AsyntaiDefaults.FeedClockSkew)
            return "The request has expired.";

        var signed = "page=" + (page ?? string.Empty)
            + "&limit=" + (limit ?? string.Empty)
            + "&ids=" + (ids ?? string.Empty)
            + "&ts=" + ts;
        var given = (sig ?? string.Empty).Trim().ToLowerInvariant();
        if (given.Length != 64)
            return "Bad signature.";

        var givenBytes = Encoding.ASCII.GetBytes(given);
        foreach (var token in tokens)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(token));
            var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signed))).ToLowerInvariant();
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), givenBytes))
                return null;
        }

        return "Bad signature.";
    }

    #endregion

    #region Utilities

    /// <summary>
    /// The store address without a trailing slash
    /// </summary>
    public static string StoreUrl(Store store)
    {
        return (store.Url ?? string.Empty).Trim().TrimEnd('/');
    }

    /// <summary>
    /// The language an anonymous visitor gets first in this store
    /// </summary>
    protected virtual async Task<int> GetLanguageIdAsync(Store store)
    {
        if (store.DefaultLanguageId > 0)
            return store.DefaultLanguageId;

        var languages = await _languageService.GetAllLanguagesAsync(storeId: store.Id);
        return languages.FirstOrDefault()?.Id ?? 0;
    }

    /// <summary>
    /// HTML to plain text
    /// </summary>
    protected virtual string Text(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = _htmlFormatter.StripTags(_htmlFormatter.ConvertHtmlToPlainText(html, decode: true));
        var lines = text.Replace("\r", string.Empty)
            .Split('\n')
            .Select(line => string.Join(" ", line.Split(new[] { ' ', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)))
            .Where(line => line.Length > 0);

        return string.Join("\n", lines).Trim();
    }

    /// <summary>
    /// An absolute address of an entity page in this store
    /// </summary>
    protected virtual async Task<string> UrlAsync<TEntity>(TEntity entity, Store store, int languageId)
        where TEntity : BaseEntity, ISlugSupported
    {
        var path = await _nopUrlHelper.RouteGenericUrlAsync(entity, languageId: languageId);
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        if (Uri.TryCreate(path, UriKind.Absolute, out _))
            return path;

        return StoreUrl(store) + "/" + path.TrimStart('/');
    }

    protected virtual async Task<IPagedList<Product>> SearchProductsAsync(Customer customer, int storeId, int[] ids, int pageIndex, int pageSize)
    {
        return await _productRepository.GetAllPagedAsync(async query =>
        {
            query = query.Where(p => p.Published && !p.Deleted && p.VisibleIndividually);
            if (ids != null)
                query = query.Where(p => ids.Contains(p.Id));
            query = await _storeMappingService.ApplyStoreMapping(query, storeId);
            query = await _aclService.ApplyAcl(query, customer);

            return query.OrderBy(p => p.Id);
        }, pageIndex, pageSize);
    }

    protected virtual async Task<IList<Category>> GetCategoriesAsync(Customer customer, int storeId)
    {
        return await _categoryRepository.GetAllAsync(async query =>
        {
            query = query.Where(c => c.Published && !c.Deleted);
            query = await _storeMappingService.ApplyStoreMapping(query, storeId);
            query = await _aclService.ApplyAcl(query, customer);

            return query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id);
        });
    }

    protected virtual async Task<IList<Manufacturer>> GetManufacturersAsync(Customer customer, int storeId)
    {
        return await _manufacturerRepository.GetAllAsync(async query =>
        {
            query = query.Where(m => m.Published && !m.Deleted);
            query = await _storeMappingService.ApplyStoreMapping(query, storeId);
            query = await _aclService.ApplyAcl(query, customer);

            return query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Id);
        });
    }

    #endregion

    #region Products

    /// <summary>
    /// One page of the product feed, or the products with the given ids
    /// </summary>
    public virtual async Task<Dictionary<string, object>> GetProductFeedAsync(Store store, string pageValue, string limitValue, string idsValue)
    {
        var customer = await _customerService.GetOrCreateSearchEngineUserAsync();
        var languageId = await GetLanguageIdAsync(store);
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        var storeUrl = StoreUrl(store);

        var answer = new Dictionary<string, object>
        {
            ["ok"] = true,
            ["store"] = new Dictionary<string, object>
            {
                ["name"] = store.Name ?? string.Empty,
                ["url"] = storeUrl,
                ["currency"] = currency?.CurrencyCode ?? string.Empty,
                ["version"] = NopVersion.FULL_VERSION,
                ["pages_url"] = storeUrl + "/" + AsyntaiDefaults.PageFeedPath
            }
        };

        var categories = await GetCategoriesAsync(customer, store.Id);
        var manufacturers = await GetManufacturersAsync(customer, store.Id);

        //lookup by id, used to check a price just before a chat answer quotes it
        if (!string.IsNullOrEmpty(idsValue))
        {
            var ids = idsValue.Split(',')
                .Select(id => int.TryParse(id.Trim(), out var value) ? value : 0)
                .Where(id => id > 0)
                .Distinct()
                .Take(AsyntaiDefaults.FeedMaxIds)
                .ToArray();
            var found = ids.Length == 0
                ? new List<Dictionary<string, object>>()
                : await BuildProductsAsync(await SearchProductsAsync(customer, store.Id, ids, 0, AsyntaiDefaults.FeedMaxIds),
                    categories, manufacturers, store, customer, currency, languageId);

            answer["page"] = 1;
            answer["pages"] = 1;
            answer["total"] = found.Count;
            answer["products"] = found;
            return answer;
        }

        var page = int.TryParse(pageValue, out var askedPage) && askedPage > 1 ? askedPage : 1;
        var size = int.TryParse(limitValue, out var askedSize) && askedSize >= 1 && askedSize <= AsyntaiDefaults.FeedMaxPageSize
            ? askedSize
            : AsyntaiDefaults.FeedPageSize;

        var products = await SearchProductsAsync(customer, store.Id, null, page - 1, size);

        answer["page"] = page;
        answer["pages"] = (products.TotalCount + size - 1) / size;
        answer["total"] = products.TotalCount;
        answer["products"] = await BuildProductsAsync(products, categories, manufacturers, store, customer, currency, languageId);

        if (page == 1)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var category in categories)
            {
                var item = new Dictionary<string, object>
                {
                    ["name"] = await _localizationService.GetLocalizedAsync(category, x => x.Name, languageId),
                    ["url"] = await UrlAsync(category, store, languageId)
                };
                var description = Text(await _localizationService.GetLocalizedAsync(category, x => x.Description, languageId));
                if (description.Length > 0)
                    item["description"] = description;
                list.Add(item);
            }

            answer["categories"] = list;
        }

        return answer;
    }

    protected virtual async Task<List<Dictionary<string, object>>> BuildProductsAsync(IList<Product> products,
        IList<Category> categories, IList<Manufacturer> manufacturers,
        Store store, Customer customer, Currency currency, int languageId)
    {
        var result = new List<Dictionary<string, object>>();
        if (products.Count == 0)
            return result;

        var productIds = products.Select(p => p.Id).ToArray();
        var categoryIds = await _categoryService.GetProductCategoryIdsAsync(productIds);
        var manufacturerIds = await _manufacturerService.GetProductManufacturerIdsAsync(productIds);

        //a store can hide prices from guests; then the feed has none either
        var showPrices = await _permissionService.AuthorizeAsync(StandardPermission.PublicStore.DISPLAY_PRICES, customer);
        var taxDisplayType = await _customerService.GetCustomerTaxDisplayTypeAsync(customer);

        foreach (var product in products)
        {
            var item = new Dictionary<string, object>
            {
                ["id"] = product.Id,
                ["name"] = await _localizationService.GetLocalizedAsync(product, x => x.Name, languageId),
                ["url"] = await UrlAsync(product, store, languageId)
            };

            var description = Text(await _localizationService.GetLocalizedAsync(product, x => x.FullDescription, languageId));
            if (description.Length == 0)
                description = Text(await _localizationService.GetLocalizedAsync(product, x => x.ShortDescription, languageId));
            if (description.Length > 0)
                item["description"] = description;

            if (showPrices && currency != null
                && !product.CallForPrice
                && !product.CustomerEntersPrice
                && product.ProductType == ProductType.SimpleProduct)
            {
                var (withoutDiscounts, finalPrice, _, _) = await _priceCalculationService.GetFinalPriceAsync(product, customer, store);
                var (price, _) = await _taxService.GetProductPriceAsync(product, withoutDiscounts, taxDisplayType == TaxDisplayType.IncludingTax, customer);
                var (special, _) = await _taxService.GetProductPriceAsync(product, finalPrice, taxDisplayType == TaxDisplayType.IncludingTax, customer);
                price = await _priceCalculationService.RoundPriceAsync(price, currency);
                special = await _priceCalculationService.RoundPriceAsync(special, currency);

                if (price > decimal.Zero)
                {
                    item["price"] = price;
                    if (special > decimal.Zero && special < price)
                        item["special"] = special;
                    item["currency"] = currency.CurrencyCode;
                }
            }

            var pictures = await _pictureService.GetPicturesByProductIdAsync(product.Id, 1);
            if (pictures.Count > 0)
                item["image_url"] = await _pictureService.GetPictureUrlAsync(pictures[0].Id, storeLocation: StoreUrl(store) + "/");

            if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
            {
                var quantity = await _productService.GetTotalStockQuantityAsync(product);
                //the count only when the store shows it to visitors
                if (product.DisplayStockAvailability && product.DisplayStockQuantity)
                    item["quantity"] = Math.Max(quantity, 0);
                if (quantity <= 0 && product.BackorderMode == BackorderMode.NoBackorders && product.DisplayStockAvailability)
                    item["stock_status"] = "Out of stock";
            }

            if (manufacturerIds.TryGetValue(product.Id, out var mIds))
            {
                var manufacturer = manufacturers.FirstOrDefault(m => mIds.Contains(m.Id));
                if (manufacturer != null)
                    item["manufacturer"] = await _localizationService.GetLocalizedAsync(manufacturer, x => x.Name, languageId);
            }

            if (categoryIds.TryGetValue(product.Id, out var cIds))
            {
                var names = new List<string>();
                foreach (var category in categories.Where(c => cIds.Contains(c.Id)))
                    names.Add(await _localizationService.GetLocalizedAsync(category, x => x.Name, languageId));
                if (names.Count > 0)
                    item["categories"] = names;
            }

            if (!string.IsNullOrWhiteSpace(product.Sku))
                item["sku"] = product.Sku.Trim();
            if (!string.IsNullOrWhiteSpace(product.ManufacturerPartNumber))
                item["mpn"] = product.ManufacturerPartNumber.Trim();
            var gtin = product.Gtin?.Trim() ?? string.Empty;
            if (gtin.Length == 12)
                item["upc"] = gtin;
            else if (gtin.Length > 0)
                item["ean"] = gtin;

            result.Add(item);
        }

        return result;
    }

    #endregion

    #region Pages

    /// <summary>
    /// One page of the page feed: the published topics a visitor can open
    /// </summary>
    public virtual async Task<Dictionary<string, object>> GetPageFeedAsync(Store store, string pageValue, string limitValue)
    {
        var languageId = await GetLanguageIdAsync(store);

        //ACL is not applied here, because that reads the current customer;
        //topics limited to customer roles are left out below instead
        var topics = await _topicService.GetAllTopicsAsync(store.Id, ignoreAcl: true);

        var items = new List<Dictionary<string, object>>();
        foreach (var topic in topics)
        {
            if (!topic.Published || topic.SubjectToAcl || topic.IsPasswordProtected)
                continue;
            if (string.Equals(topic.SystemName, "PageNotFound", StringComparison.OrdinalIgnoreCase))
                continue;

            var title = Text(await _localizationService.GetLocalizedAsync(topic, x => x.Title, languageId));
            var content = Text(await _localizationService.GetLocalizedAsync(topic, x => x.Body, languageId));
            if (title.Length == 0 || content.Length == 0)
                continue;

            var url = await UrlAsync(topic, store, languageId);
            if (url.Length == 0)
                continue;

            items.Add(new Dictionary<string, object>
            {
                ["type"] = "page",
                ["title"] = title,
                ["url"] = url,
                ["content"] = content
            });
        }

        var page = int.TryParse(pageValue, out var askedPage) && askedPage > 1 ? askedPage : 1;
        var size = int.TryParse(limitValue, out var askedSize) && askedSize >= 1 && askedSize <= AsyntaiDefaults.FeedMaxPageSize
            ? askedSize
            : AsyntaiDefaults.FeedPageSize;

        return new Dictionary<string, object>
        {
            ["ok"] = true,
            ["page"] = page,
            ["pages"] = (items.Count + size - 1) / size,
            ["total"] = items.Count,
            ["items"] = items.Skip((page - 1) * size).Take(size).ToList()
        };
    }

    #endregion
}
