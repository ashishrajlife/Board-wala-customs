using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ValousWorld.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    AnnouncementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Text = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.AnnouncementId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OtpLogs",
                columns: table => new
                {
                    OtpLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    OtpCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Purpose = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpLogs", x => x.OtpLogId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RoleName = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EventId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true),
                    RazorpayOrderId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RazorpayPaymentId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Payload = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ShortDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    MRP = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    SavePercent = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Sizes = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Colors = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Image1 = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Image2 = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Image3 = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    Image4 = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    VideoUrl = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    SKU = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Composition = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    GSM = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    PrintType = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Neckline = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    FitType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    CountryOfProduction = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    WashCare = table.Column<string>(type: "longtext", nullable: true),
                    SizingNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    MRPNote = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    AdditionalNote = table.Column<string>(type: "longtext", nullable: true),
                    Tags = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Benefits = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    VoucherCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    VoucherDiscountPercent = table.Column<int>(type: "int", nullable: true),
                    VoucherMinOrderValue = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    VoucherMaxDiscount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    VoucherValidUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VoucherIsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsFeatured = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsNewArrival = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WeightGrams = table.Column<int>(type: "int", nullable: false),
                    LengthCm = table.Column<int>(type: "int", nullable: true),
                    WidthCm = table.Column<int>(type: "int", nullable: true),
                    HeightCm = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FullName = table.Column<string>(type: "longtext", nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", nullable: true),
                    Password = table.Column<string>(type: "longtext", nullable: true),
                    Phone = table.Column<string>(type: "varchar(255)", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    AddressId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Line1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Line2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Pincode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.AddressId);
                    table.ForeignKey(
                        name: "FK_Addresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    CartId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.CartId);
                    table.ForeignKey(
                        name: "FK_Carts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    OrderNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Shipping = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    RazorpayOrderId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RazorpayPaymentId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RazorpaySignature = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    PaymentAttempts = table.Column<int>(type: "int", nullable: false),
                    LastPaymentAttemptAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FailureReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RefundId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RefundedAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ShippingFullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ShippingPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ShippingLine1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ShippingLine2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ShippingCity = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ShippingState = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ShippingPincode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    ShippingCountry = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CourierName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DelhiveryWaybill = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    DelhiveryStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    LastTrackingSyncAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CodFee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CodCollectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CancelledBy = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CancellationReason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    RefundStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    PlacedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "longtext", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsRevoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    CartItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CartId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OriginalMRP = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.CartItemId);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "CartId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ProductImage = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    Size = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OriginalMRP = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.OrderItemId);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    PaymentAttemptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    RazorpayOrderId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RazorpayPaymentId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ErrorDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    ErrorReason = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ErrorStep = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.PaymentAttemptId);
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_DisplayOrder",
                table: "Announcements",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsActive",
                table: "Announcements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId_Size_Color",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId", "Size", "Color" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryId",
                table: "Categories",
                column: "CategoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpLogs_ExpiresAt",
                table: "OtpLogs",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OtpLogs_Phone",
                table: "OtpLogs",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_OrderId",
                table: "PaymentAttempts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_RazorpayPaymentId",
                table: "PaymentAttempts",
                column: "RazorpayPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductId",
                table: "Products",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "`Email` IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true,
                filter: "`Phone` IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EventId",
                table: "WebhookEvents",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "OtpLogs");

            migrationBuilder.DropTable(
                name: "PaymentAttempts");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "WebhookEvents");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
