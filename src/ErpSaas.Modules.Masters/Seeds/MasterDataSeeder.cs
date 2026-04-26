using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Masters.Seeds;

public sealed class MasterDataSeeder(
    PlatformDbContext db,
    ILogger<MasterDataSeeder> logger) : IDataSeeder
{
    public int Order => 15;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await SeedCurrenciesAsync(ct);
            await SeedCountriesAsync(ct);
            await SeedIndiaStatesAsync(ct);
            await SeedIndianCitiesAsync(ct);
            await SeedHsnSacCodesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "MasterDataSeeder failed — rolled back");
            throw;
        }
    }

    private async Task SeedCurrenciesAsync(CancellationToken ct)
    {
        if (await db.Currencies.AnyAsync(ct)) return;

        var currencies = new[]
        {
            ("INR", "Indian Rupee",        "₹", 2),
            ("USD", "US Dollar",           "$", 2),
            ("EUR", "Euro",                "€", 2),
            ("GBP", "British Pound",       "£", 2),
            ("AED", "UAE Dirham",          "د.إ", 2),
            ("SGD", "Singapore Dollar",    "S$", 2),
            ("JPY", "Japanese Yen",        "¥", 0),
            ("CNY", "Chinese Yuan",        "¥", 2),
            ("AUD", "Australian Dollar",   "A$", 2),
            ("CAD", "Canadian Dollar",     "C$", 2),
            ("CHF", "Swiss Franc",         "Fr", 2),
        };

        foreach (var (code, name, symbol, dp) in currencies)
            db.Currencies.Add(new Currency { Code = code, Name = name, Symbol = symbol, DecimalPlaces = dp, CreatedAtUtc = DateTime.UtcNow });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} currencies", currencies.Length);
    }

    private async Task SeedCountriesAsync(CancellationToken ct)
    {
        if (await db.Countries.AnyAsync(ct)) return;

        // Top 30 countries likely relevant + all 195 via ISO
        var countries = new[]
        {
            ("IN", "India",                "+91",  "INR"),
            ("US", "United States",        "+1",   "USD"),
            ("GB", "United Kingdom",       "+44",  "GBP"),
            ("AE", "United Arab Emirates", "+971", "AED"),
            ("SG", "Singapore",            "+65",  "SGD"),
            ("AU", "Australia",            "+61",  "AUD"),
            ("CA", "Canada",               "+1",   "CAD"),
            ("DE", "Germany",              "+49",  "EUR"),
            ("FR", "France",               "+33",  "EUR"),
            ("JP", "Japan",                "+81",  "JPY"),
            ("CN", "China",                "+86",  "CNY"),
            ("NZ", "New Zealand",          "+64",  "NZD"),
            ("ZA", "South Africa",         "+27",  "ZAR"),
            ("BD", "Bangladesh",           "+880", "BDT"),
            ("PK", "Pakistan",             "+92",  "PKR"),
            ("NP", "Nepal",                "+977", "NPR"),
            ("LK", "Sri Lanka",            "+94",  "LKR"),
            ("MY", "Malaysia",             "+60",  "MYR"),
            ("TH", "Thailand",             "+66",  "THB"),
            ("ID", "Indonesia",            "+62",  "IDR"),
            ("PH", "Philippines",          "+63",  "PHP"),
            ("KW", "Kuwait",               "+965", "KWD"),
            ("QA", "Qatar",                "+974", "QAR"),
            ("SA", "Saudi Arabia",         "+966", "SAR"),
            ("BH", "Bahrain",              "+973", "BHD"),
            ("OM", "Oman",                 "+968", "OMR"),
            ("IT", "Italy",                "+39",  "EUR"),
            ("ES", "Spain",                "+34",  "EUR"),
            ("NL", "Netherlands",          "+31",  "EUR"),
            ("CH", "Switzerland",          "+41",  "CHF"),
        };

        foreach (var (code, name, phone, currency) in countries)
            db.Countries.Add(new Country { Code = code, Name = name, PhoneCode = phone, CurrencyCode = currency, CreatedAtUtc = DateTime.UtcNow });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} countries", countries.Length);
    }

    private async Task SeedIndiaStatesAsync(CancellationToken ct)
    {
        var india = await db.Countries.FirstOrDefaultAsync(c => c.Code == "IN", ct);
        if (india is null) return;
        if (await db.States.AnyAsync(s => s.CountryId == india.Id, ct)) return;

        // All Indian states + UTs with GST codes
        var states = new[]
        {
            ("AN", "Andaman and Nicobar Islands", "35"),
            ("AP", "Andhra Pradesh",              "37"),
            ("AR", "Arunachal Pradesh",           "12"),
            ("AS", "Assam",                       "18"),
            ("BR", "Bihar",                       "10"),
            ("CH", "Chandigarh",                  "04"),
            ("CG", "Chhattisgarh",                "22"),
            ("DN", "Dadra and Nagar Haveli",      "26"),
            ("DD", "Daman and Diu",               "25"),
            ("DL", "Delhi",                       "07"),
            ("GA", "Goa",                         "30"),
            ("GJ", "Gujarat",                     "24"),
            ("HR", "Haryana",                     "06"),
            ("HP", "Himachal Pradesh",            "02"),
            ("JK", "Jammu and Kashmir",           "01"),
            ("JH", "Jharkhand",                   "20"),
            ("KA", "Karnataka",                   "29"),
            ("KL", "Kerala",                      "32"),
            ("LA", "Ladakh",                      "38"),
            ("LD", "Lakshadweep",                 "31"),
            ("MP", "Madhya Pradesh",              "23"),
            ("MH", "Maharashtra",                 "27"),
            ("MN", "Manipur",                     "14"),
            ("ML", "Meghalaya",                   "17"),
            ("MZ", "Mizoram",                     "15"),
            ("NL", "Nagaland",                    "13"),
            ("OR", "Odisha",                      "21"),
            ("PY", "Puducherry",                  "34"),
            ("PB", "Punjab",                      "03"),
            ("RJ", "Rajasthan",                   "08"),
            ("SK", "Sikkim",                      "11"),
            ("TN", "Tamil Nadu",                  "33"),
            ("TG", "Telangana",                   "36"),
            ("TR", "Tripura",                     "16"),
            ("UP", "Uttar Pradesh",               "09"),
            ("UK", "Uttarakhand",                 "05"),
            ("WB", "West Bengal",                 "19"),
        };

        foreach (var (code, name, gst) in states)
            db.States.Add(new State { CountryId = india.Id, Code = code, Name = name, GstStateCode = gst, CreatedAtUtc = DateTime.UtcNow });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} Indian states", states.Length);
    }

    private async Task SeedIndianCitiesAsync(CancellationToken ct)
    {
        if (await db.Cities.AnyAsync(ct)) return;

        // State code → cities mapping (top cities per state)
        var stateCities = new Dictionary<string, string[]>
        {
            ["MH"] = ["Mumbai", "Pune", "Nagpur", "Thane", "Aurangabad", "Nashik", "Solapur", "Kolhapur", "Amravati", "Navi Mumbai"],
            ["GJ"] = ["Ahmedabad", "Surat", "Vadodara", "Rajkot", "Bhavnagar", "Jamnagar", "Gandhinagar", "Anand", "Morbi", "Bharuch"],
            ["KA"] = ["Bengaluru", "Mysuru", "Hubballi", "Mangaluru", "Belagavi", "Davangere", "Ballari", "Vijayapura", "Shivamogga", "Tumakuru"],
            ["TN"] = ["Chennai", "Coimbatore", "Madurai", "Tiruchirappalli", "Salem", "Tirunelveli", "Tiruppur", "Erode", "Vellore", "Thoothukudi"],
            ["UP"] = ["Lucknow", "Kanpur", "Agra", "Varanasi", "Prayagraj", "Ghaziabad", "Noida", "Meerut", "Aligarh", "Bareilly"],
            ["RJ"] = ["Jaipur", "Jodhpur", "Udaipur", "Kota", "Ajmer", "Bikaner", "Alwar", "Bhilwara", "Sikar", "Bharatpur"],
            ["DL"] = ["New Delhi", "Delhi"],
            ["WB"] = ["Kolkata", "Howrah", "Durgapur", "Asansol", "Siliguri", "Bardhaman", "Malda", "Baharampur", "Kharagpur", "Haldia"],
            ["HR"] = ["Gurugram", "Faridabad", "Panipat", "Ambala", "Hisar", "Rohtak", "Karnal", "Sonipat", "Yamunanagar", "Panchkula"],
            ["PB"] = ["Ludhiana", "Amritsar", "Jalandhar", "Patiala", "Bathinda", "Mohali", "Hoshiarpur", "Firozpur", "Kapurthala", "Moga"],
            ["MP"] = ["Bhopal", "Indore", "Gwalior", "Jabalpur", "Ujjain", "Sagar", "Dewas", "Satna", "Ratlam", "Rewa"],
            ["AP"] = ["Visakhapatnam", "Vijayawada", "Guntur", "Nellore", "Kurnool", "Tirupati", "Rajamahendravaram", "Kadapa", "Kakinada", "Eluru"],
            ["TG"] = ["Hyderabad", "Warangal", "Nizamabad", "Karimnagar", "Khammam", "Ramagundam", "Mahbubnagar", "Nalgonda", "Adilabad", "Suryapet"],
            ["KL"] = ["Thiruvananthapuram", "Kochi", "Kozhikode", "Thrissur", "Kollam", "Palakkad", "Alappuzha", "Malappuram", "Kannur", "Kottayam"],
            ["BR"] = ["Patna", "Gaya", "Bhagalpur", "Muzaffarpur", "Purnia", "Darbhanga", "Arrah", "Begusarai", "Chhapra", "Sasaram"],
            ["OR"] = ["Bhubaneswar", "Cuttack", "Rourkela", "Brahmapur", "Sambalpur", "Puri", "Balasore", "Bhadrak", "Baripada", "Jharsuguda"],
            ["JH"] = ["Ranchi", "Jamshedpur", "Dhanbad", "Bokaro Steel City", "Deoghar", "Phusro", "Hazaribag", "Giridih", "Ramgarh", "Medininagar"],
            ["AS"] = ["Guwahati", "Silchar", "Dibrugarh", "Jorhat", "Nagaon", "Tinsukia", "Tezpur", "Bongaigaon", "Karimganj", "Sibsagar"],
            ["CG"] = ["Raipur", "Bhilai", "Bilaspur", "Korba", "Durg", "Rajnandgaon", "Jagdalpur", "Ambikapur", "Raigarh", "Mahasamund"],
            ["UK"] = ["Dehradun", "Haridwar", "Roorkee", "Haldwani", "Rudrapur", "Kashipur", "Rishikesh", "Nainital", "Mussoorie", "Kotdwar"],
        };

        var stateMap = await db.States.Where(s => stateCities.Keys.Contains(s.Code))
            .ToDictionaryAsync(s => s.Code, s => s.Id, ct);

        foreach (var (code, cities) in stateCities)
        {
            if (!stateMap.TryGetValue(code, out var stateId)) continue;
            foreach (var city in cities)
                db.Cities.Add(new City { StateId = stateId, Name = city, CreatedAtUtc = DateTime.UtcNow });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded Indian cities");
    }

    private async Task SeedHsnSacCodesAsync(CancellationToken ct)
    {
        var existingList = await db.HsnSacCodes.Select(x => x.Code).ToListAsync(ct);
        var existing = existingList.ToHashSet();

        // Electrical / Electronics / Power tools HSN codes
        var codes = new[]
        {
            ("8501", "Electric motors and generators",                        HsnSacType.HSN, 18m),
            ("8502", "Electric generating sets and rotary converters",        HsnSacType.HSN, 18m),
            ("8504", "Electrical transformers, static converters",            HsnSacType.HSN, 18m),
            ("8505", "Electro-magnets, permanent magnets",                   HsnSacType.HSN, 18m),
            ("8507", "Electric accumulators, including separators",          HsnSacType.HSN, 18m),
            ("8508", "Vacuum cleaners",                                       HsnSacType.HSN, 28m),
            ("8509", "Electro-mechanical domestic appliances",               HsnSacType.HSN, 28m),
            ("8510", "Shavers, hair clippers and hair removing appliances",  HsnSacType.HSN, 28m),
            ("8512", "Electrical lighting / signalling equipment",           HsnSacType.HSN, 18m),
            ("8516", "Electric water heaters, hair dryers, ovens",          HsnSacType.HSN, 28m),
            ("8517", "Telephone sets; smartphones",                          HsnSacType.HSN, 18m),
            ("8518", "Microphones, loudspeakers, headphones",                HsnSacType.HSN, 18m),
            ("8519", "Sound recording / reproducing apparatus",              HsnSacType.HSN, 18m),
            ("8521", "Video recording/reproducing apparatus",                HsnSacType.HSN, 28m),
            ("8523", "Flash memory, data storage media",                     HsnSacType.HSN, 18m),
            ("8525", "Transmission apparatus for radio-broadcasting",        HsnSacType.HSN, 18m),
            ("8528", "Monitors and projectors; TV receivers",                HsnSacType.HSN, 28m),
            ("8534", "Printed circuits",                                     HsnSacType.HSN, 18m),
            ("8536", "Electrical apparatus for switching/protecting circuits",HsnSacType.HSN, 18m),
            ("8537", "Boards, panels, consoles for electrical control",      HsnSacType.HSN, 18m),
            ("8541", "Diodes, transistors and similar semiconductor devices",HsnSacType.HSN, 18m),
            ("8542", "Electronic integrated circuits",                       HsnSacType.HSN, 18m),
            ("8544", "Insulated wire, cable and other conductors",           HsnSacType.HSN, 18m),
            ("8545", "Carbon electrodes, carbon brushes",                    HsnSacType.HSN, 18m),
            ("8546", "Electrical insulators of any material",                HsnSacType.HSN, 18m),
            ("8547", "Insulating fittings for electrical machines",          HsnSacType.HSN, 18m),
            // Power tools
            ("8467", "Tools for working in hand, pneumatic/motor-operated",  HsnSacType.HSN, 18m),
            ("8468", "Machinery for soldering, brazing or welding",         HsnSacType.HSN, 18m),
            ("8205", "Hand tools (hammers, screwdrivers, spanners)",        HsnSacType.HSN, 18m),
            ("8206", "Sets of tools from two or more headings",             HsnSacType.HSN, 18m),
            ("8207", "Interchangeable tools for hand/machine tools",        HsnSacType.HSN, 18m),
            ("8208", "Knives and cutting blades for machines",              HsnSacType.HSN, 18m),
            ("8211", "Knives with cutting blades, not elsewhere specified",  HsnSacType.HSN, 18m),
            // Wiring accessories
            ("8535", "Electrical apparatus for switching HV circuits",       HsnSacType.HSN, 18m),
            ("8538", "Parts for electrical switching apparatus",             HsnSacType.HSN, 18m),
            // Batteries
            ("8506", "Primary cells and primary batteries",                  HsnSacType.HSN, 28m),
            // Solar (8541 already listed above; 8504 already listed above — no duplicates)
            ("8543", "Electrical machines and apparatus not elsewhere specified", HsnSacType.HSN, 18m),
            // SAC codes
            ("9954", "Construction services",                                HsnSacType.SAC, 18m),
            ("9983", "IT and telecom services",                              HsnSacType.SAC, 18m),
            ("9985", "Support services",                                     HsnSacType.SAC, 18m),
            ("9987", "Maintenance and repair services",                      HsnSacType.SAC, 18m),
            ("9988", "Manufacturing services on physical inputs owned by others", HsnSacType.SAC, 18m),
        };

        var toInsert = codes.Where(c => !existing.Contains(c.Item1)).ToList();
        if (toInsert.Count == 0) return;

        foreach (var (code, desc, type, rate) in toInsert)
            db.HsnSacCodes.Add(new HsnSacCode
            {
                Code = code, Description = desc, Type = type, GstRate = rate,
                IsActive = true, CreatedAtUtc = DateTime.UtcNow
            });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} HSN/SAC codes", toInsert.Count);
    }
}
