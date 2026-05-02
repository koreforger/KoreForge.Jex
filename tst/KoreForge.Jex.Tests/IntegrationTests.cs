using Xunit;
using KoreForge.Jex;
using Newtonsoft.Json.Linq;

namespace KoreForge.Jex.Tests;

/// <summary>
/// End-to-end integration tests demonstrating JEX transforming complex JSON documents.
/// These tests showcase the full feature set of the JEX language.
/// </summary>
public class IntegrationTests
{
    private readonly Jex _jex = new();

    /// <summary>
    /// Transform a shopping cart into an order summary with calculations.
    /// Demonstrates: loops, arithmetic, conditionals, object creation, JSONPath.
    /// </summary>
    [Fact]
    public void Integration_ShoppingCartToOrderSummary()
    {
        var script = @"
            // Calculate cart totals
            %let items = jpAll($in, ""$.cart.items[*]"");
            %let subtotal = 0;
            %let itemCount = 0;
            
            %foreach item %in &items %do;
                %let price = jp1(&item, ""$.price"");
                %let qty = jp1(&item, ""$.quantity"");
                %let subtotal = &subtotal + (&price * &qty);
                %let itemCount = &itemCount + &qty;
            %end;
            
            // Apply discount based on subtotal
            %let discountRate = 0;
            %if (&subtotal >= 100) %then %do;
                %let discountRate = 0.1;
            %end;
            %let discount = &subtotal * &discountRate;
            %let total = &subtotal - &discount;
            
            // Build order summary
            %set $.orderId = concat(""ORD-"", jp1($in, ""$.customer.id""));
            %set $.customerName = jp1($in, ""$.customer.name"");
            %set $.itemCount = &itemCount;
            %set $.subtotal = round(&subtotal, 2);
            %set $.discount = round(&discount, 2);
            %set $.total = round(&total, 2);
            %set $.qualifiesForFreeShipping = &total >= 50;
        ";

        var input = JObject.Parse(@"{
            ""customer"": {
                ""id"": ""12345"",
                ""name"": ""Jane Doe""
            },
            ""cart"": {
                ""items"": [
                    { ""name"": ""Widget"", ""price"": 29.99, ""quantity"": 2 },
                    { ""name"": ""Gadget"", ""price"": 49.99, ""quantity"": 1 },
                    { ""name"": ""Gizmo"", ""price"": 15.00, ""quantity"": 3 }
                ]
            }
        }");

        var result = _jex.Execute(script, input);

        Assert.Equal("ORD-12345", result["orderId"]?.Value<string>());
        Assert.Equal("Jane Doe", result["customerName"]?.Value<string>());
        Assert.Equal(6, result["itemCount"]?.Value<int>());
        Assert.Equal(154.97m, result["subtotal"]?.Value<decimal>());
        Assert.Equal(15.50m, result["discount"]?.Value<decimal>());
        Assert.Equal(139.47m, result["total"]?.Value<decimal>());
        Assert.True(result["qualifiesForFreeShipping"]?.Value<bool>());
    }

    /// <summary>
    /// Transform an API response into a normalized format with error handling.
    /// Demonstrates: conditionals, null handling, string manipulation, nested objects.
    /// </summary>
    [Fact]
    public void Integration_ApiResponseNormalization()
    {
        var script = @"
            // Check if response was successful
            %let status = jp1($in, ""$.status"");
            %let success = &status == ""success"" || &status == ""ok"";
            
            %set $.normalized.success = &success;
            %set $.normalized.timestamp = jp1($in, ""$.timestamp"");
            
            %if (&success) %then %do;
                // Extract and transform user data
                %let users = jpAll($in, ""$.data.users[*]"");
                %let userList = arr();
                
                %foreach user %in &users %do;
                    %let fullName = concat(jp1(&user, ""$.firstName""), "" "", jp1(&user, ""$.lastName""));
                    %let email = lower(jp1(&user, ""$.email""));
                    %let isActive = jp1(&user, ""$.status"") == ""active"";
                    
                    %let normalizedUser = obj(
                        ""id"", jp1(&user, ""$.id""),
                        ""displayName"", &fullName,
                        ""email"", &email,
                        ""active"", &isActive
                    );
                    push(&userList, &normalizedUser);
                %end;
                
                %set $.normalized.users = &userList;
                %set $.normalized.userCount = length(&userList);
                %set $.normalized.error = null;
            %end;
            %else %do;
                %set $.normalized.users = arr();
                %set $.normalized.userCount = 0;
                %set $.normalized.error = jp1($in, ""$.message"");
            %end;
        ";

        var input = JObject.Parse(@"{
            ""status"": ""success"",
            ""timestamp"": ""2024-01-27T10:30:00Z"",
            ""data"": {
                ""users"": [
                    { ""id"": 1, ""firstName"": ""John"", ""lastName"": ""Smith"", ""email"": ""JOHN@EXAMPLE.COM"", ""status"": ""active"" },
                    { ""id"": 2, ""firstName"": ""Alice"", ""lastName"": ""Jones"", ""email"": ""Alice@Example.COM"", ""status"": ""inactive"" }
                ]
            }
        }");

        var result = _jex.Execute(script, input);

        Assert.True(result["normalized"]?["success"]?.Value<bool>());
        Assert.Equal(2, result["normalized"]?["userCount"]?.Value<int>());
        
        var users = result["normalized"]?["users"] as JArray;
        Assert.NotNull(users);
        Assert.Equal(2, users!.Count);
        
        Assert.Equal("John Smith", users[0]?["displayName"]?.Value<string>());
        Assert.Equal("john@example.com", users[0]?["email"]?.Value<string>());
        Assert.True(users[0]?["active"]?.Value<bool>());
        
        Assert.Equal("Alice Jones", users[1]?["displayName"]?.Value<string>());
        Assert.False(users[1]?["active"]?.Value<bool>());
    }

    /// <summary>
    /// Generate a report from transaction data with grouping and aggregation.
    /// Demonstrates: nested loops, accumulation, macros, array building.
    /// </summary>
    [Fact]
    public void Integration_TransactionReportGeneration()
    {
        var script = @"
            // Helper function to categorize amount
            %func getAmountCategory(amount);
                %if (&amount < 100) %then %do;
                    %return ""small"";
                %end;
                %if (&amount < 500) %then %do;
                    %return ""medium"";
                %end;
                %return ""large"";
            %endfunc;

            %let transactions = jpAll($in, ""$.transactions[*]"");
            
            // Initialize counters
            %let totalAmount = 0;
            %let creditTotal = 0;
            %let debitTotal = 0;
            %let txnCount = 0;
            %let smallCount = 0;
            %let mediumCount = 0;
            %let largeCount = 0;
            
            // Process transactions
            %foreach txn %in &transactions %do;
                %let amount = jp1(&txn, ""$.amount"");
                %let type = jp1(&txn, ""$.type"");
                %let txnCount = &txnCount + 1;
                
                %if (&type == ""credit"") %then %do;
                    %let creditTotal = &creditTotal + &amount;
                    %let totalAmount = &totalAmount + &amount;
                %end;
                %else %do;
                    %let debitTotal = &debitTotal + &amount;
                    %let totalAmount = &totalAmount - &amount;
                %end;
                
                // Categorize
                %let category = getAmountCategory(&amount);
                %if (&category == ""small"") %then %do;
                    %let smallCount = &smallCount + 1;
                %end;
                %if (&category == ""medium"") %then %do;
                    %let mediumCount = &mediumCount + 1;
                %end;
                %if (&category == ""large"") %then %do;
                    %let largeCount = &largeCount + 1;
                %end;
            %end;
            
            // Build report
            %set $.report.accountId = jp1($in, ""$.accountId"");
            %set $.report.period = jp1($in, ""$.period"");
            %set $.report.transactionCount = &txnCount;
            %set $.report.summary.totalCredits = round(&creditTotal, 2);
            %set $.report.summary.totalDebits = round(&debitTotal, 2);
            %set $.report.summary.netBalance = round(&totalAmount, 2);
            %set $.report.breakdown.small = &smallCount;
            %set $.report.breakdown.medium = &mediumCount;
            %set $.report.breakdown.large = &largeCount;
        ";

        var input = JObject.Parse(@"{
            ""accountId"": ""ACC-789"",
            ""period"": ""2024-Q1"",
            ""transactions"": [
                { ""id"": ""T1"", ""type"": ""credit"", ""amount"": 50.00 },
                { ""id"": ""T2"", ""type"": ""credit"", ""amount"": 250.00 },
                { ""id"": ""T3"", ""type"": ""debit"", ""amount"": 75.00 },
                { ""id"": ""T4"", ""type"": ""credit"", ""amount"": 1000.00 },
                { ""id"": ""T5"", ""type"": ""debit"", ""amount"": 200.00 }
            ]
        }");

        var result = _jex.Execute(script, input);

        Assert.Equal("ACC-789", result["report"]?["accountId"]?.Value<string>());
        Assert.Equal("2024-Q1", result["report"]?["period"]?.Value<string>());
        Assert.Equal(5, result["report"]?["transactionCount"]?.Value<int>());
        Assert.Equal(1300.00m, result["report"]?["summary"]?["totalCredits"]?.Value<decimal>());
        Assert.Equal(275.00m, result["report"]?["summary"]?["totalDebits"]?.Value<decimal>());
        Assert.Equal(1025.00m, result["report"]?["summary"]?["netBalance"]?.Value<decimal>());
        Assert.Equal(2, result["report"]?["breakdown"]?["small"]?.Value<int>()); // 50, 75
        Assert.Equal(2, result["report"]?["breakdown"]?["medium"]?.Value<int>()); // 250, 200
        Assert.Equal(1, result["report"]?["breakdown"]?["large"]?.Value<int>()); // 1000
    }

    /// <summary>
    /// Transform a product catalog with filtering and restructuring.
    /// Demonstrates: filtering, array manipulation, library usage.
    /// </summary>
    [Fact]
    public void Integration_ProductCatalogTransformation()
    {
        var jex = new Jex();
        
        // Load a utility library with helper functions
        var utilLib = @"
            %func isInStock(qty);
                %return &qty > 0;
            %endfunc;

            %func getStockStatus(qty);
                %if (&qty == 0) %then %do;
                    %return ""Out of Stock"";
                %end;
                %if (&qty < 10) %then %do;
                    %return ""Low Stock"";
                %end;
                %return ""In Stock"";
            %endfunc;
        ";
        jex.LoadLibraryFromSource("ProductUtils", utilLib);

        var script = @"
            %let products = jpAll($in, ""$.catalog.products[*]"");
            %let minPrice = jp1($in, ""$.filters.minPrice"");
            %let maxPrice = jp1($in, ""$.filters.maxPrice"");
            %let categoryFilter = jp1($in, ""$.filters.category"");
            
            %let filtered = arr();
            %let totalValue = 0;
            
            %foreach product %in &products %do;
                %let price = jp1(&product, ""$.price"");
                %let qty = jp1(&product, ""$.quantity"");
                %let cat = jp1(&product, ""$.category"");
                
                // Apply filters
                %let matchesPrice = &price >= &minPrice && &price <= &maxPrice;
                %let matchesCategory = &categoryFilter == ""all"" || &cat == &categoryFilter;
                
                %if (&matchesPrice && &matchesCategory) %then %do;
                    %let displayProduct = obj(
                        ""id"", jp1(&product, ""$.id""),
                        ""name"", jp1(&product, ""$.name""),
                        ""category"", &cat,
                        ""price"", &price,
                        ""stockStatus"", getStockStatus(&qty),
                        ""available"", isInStock(&qty)
                    );
                    push(&filtered, &displayProduct);
                    
                    %if (isInStock(&qty)) %then %do;
                        %let totalValue = &totalValue + (&price * &qty);
                    %end;
                %end;
            %end;
            
            %set $.results.products = &filtered;
            %set $.results.count = length(&filtered);
            %set $.results.totalInventoryValue = round(&totalValue, 2);
        ";

        var input = JObject.Parse(@"{
            ""catalog"": {
                ""products"": [
                    { ""id"": ""P1"", ""name"": ""Widget Pro"", ""category"": ""electronics"", ""price"": 99.99, ""quantity"": 50 },
                    { ""id"": ""P2"", ""name"": ""Basic Widget"", ""category"": ""electronics"", ""price"": 29.99, ""quantity"": 5 },
                    { ""id"": ""P3"", ""name"": ""Premium Gadget"", ""category"": ""electronics"", ""price"": 199.99, ""quantity"": 0 },
                    { ""id"": ""P4"", ""name"": ""Office Chair"", ""category"": ""furniture"", ""price"": 149.99, ""quantity"": 12 },
                    { ""id"": ""P5"", ""name"": ""Desk Lamp"", ""category"": ""furniture"", ""price"": 45.00, ""quantity"": 30 }
                ]
            },
            ""filters"": {
                ""minPrice"": 25.00,
                ""maxPrice"": 150.00,
                ""category"": ""electronics""
            }
        }");

        var result = jex.Execute(script, input);

        Assert.Equal(2, result["results"]?["count"]?.Value<int>()); // Widget Pro and Basic Widget
        
        var products = result["results"]?["products"] as JArray;
        Assert.NotNull(products);
        Assert.Equal(2, products!.Count);
        
        // Widget Pro
        Assert.Equal(99.99m, products[0]?["price"]?.Value<decimal>());
        Assert.Equal("In Stock", products[0]?["stockStatus"]?.Value<string>());
        Assert.True(products[0]?["available"]?.Value<bool>());
        
        // Basic Widget (low stock)
        Assert.Equal(29.99m, products[1]?["price"]?.Value<decimal>());
        Assert.Equal("Low Stock", products[1]?["stockStatus"]?.Value<string>());
    }

    /// <summary>
    /// Transform nested employee data into a summary report.
    /// Demonstrates: nested data traversal, aggregation, recursive logic.
    /// </summary>
    [Fact]
    public void Integration_EmployeeDataTransformation()
    {
        var script = @"
            // Helper to count employees recursively
            %func countTeam(emp);
                %let reports = jpAll(&emp, ""$.directReports[*]"");
                %let count = 1;
                %foreach report %in &reports %do;
                    %let count = &count + countTeam(&report);
                %end;
                %return &count;
            %endfunc;

            // Process the organization
            %let ceo = jp1($in, ""$.organization.ceo"");
            %let totalEmployees = countTeam(&ceo);
            
            // Build executive summary
            %let executives = arr();
            %let ceoReports = jpAll(&ceo, ""$.directReports[*]"");
            
            %foreach exec %in &ceoReports %do;
                %let execData = obj(
                    ""name"", jp1(&exec, ""$.name""),
                    ""title"", jp1(&exec, ""$.title""),
                    ""department"", jp1(&exec, ""$.department""),
                    ""teamSize"", countTeam(&exec)
                );
                push(&executives, &execData);
            %end;
            
            %set $.summary.companyName = jp1($in, ""$.organization.name"");
            %set $.summary.ceoName = jp1(&ceo, ""$.name"");
            %set $.summary.totalHeadcount = &totalEmployees;
            %set $.summary.executives = &executives;
            %set $.summary.executiveCount = length(&executives);
        ";

        var input = JObject.Parse(@"{
            ""organization"": {
                ""name"": ""Acme Corp"",
                ""ceo"": {
                    ""id"": ""E001"",
                    ""name"": ""Sarah CEO"",
                    ""title"": ""Chief Executive Officer"",
                    ""department"": ""Executive"",
                    ""directReports"": [
                        {
                            ""id"": ""E002"",
                            ""name"": ""John CTO"",
                            ""title"": ""Chief Technology Officer"",
                            ""department"": ""Technology"",
                            ""directReports"": [
                                { ""id"": ""E004"", ""name"": ""Alice Dev"", ""title"": ""Senior Developer"", ""department"": ""Engineering"", ""directReports"": [] },
                                { ""id"": ""E005"", ""name"": ""Bob DevOps"", ""title"": ""DevOps Engineer"", ""department"": ""Operations"", ""directReports"": [] }
                            ]
                        },
                        {
                            ""id"": ""E003"",
                            ""name"": ""Mary CFO"",
                            ""title"": ""Chief Financial Officer"",
                            ""department"": ""Finance"",
                            ""directReports"": [
                                { ""id"": ""E006"", ""name"": ""Charlie Acc"", ""title"": ""Senior Accountant"", ""department"": ""Accounting"", ""directReports"": [] }
                            ]
                        }
                    ]
                }
            }
        }");

        var result = _jex.Execute(script, input);

        Assert.Equal("Acme Corp", result["summary"]?["companyName"]?.Value<string>());
        Assert.Equal("Sarah CEO", result["summary"]?["ceoName"]?.Value<string>());
        Assert.Equal(6, result["summary"]?["totalHeadcount"]?.Value<int>());
        Assert.Equal(2, result["summary"]?["executiveCount"]?.Value<int>());
        
        var executives = result["summary"]?["executives"] as JArray;
        Assert.NotNull(executives);
        Assert.Equal(2, executives!.Count);
        
        // CTO with 3 people total (himself + 2 reports)
        Assert.Equal("John CTO", executives[0]?["name"]?.Value<string>());
        Assert.Equal(3, executives[0]?["teamSize"]?.Value<int>());
        
        // CFO with 2 people total (herself + 1 report)
        Assert.Equal("Mary CFO", executives[1]?["name"]?.Value<string>());
        Assert.Equal(2, executives[1]?["teamSize"]?.Value<int>());
    }
}
