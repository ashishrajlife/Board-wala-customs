-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: localhost    Database: valousworlddatabase
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20261012161846_InitialMySql','10.0.12');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `addresses`
--

DROP TABLE IF EXISTS `addresses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `addresses` (
  `AddressId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `FullName` varchar(100) NOT NULL,
  `Phone` varchar(20) NOT NULL,
  `Line1` varchar(200) NOT NULL,
  `Line2` varchar(200) DEFAULT NULL,
  `City` varchar(100) NOT NULL,
  `State` varchar(100) NOT NULL,
  `Pincode` varchar(10) NOT NULL,
  `Country` varchar(100) NOT NULL,
  `IsDefault` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`AddressId`),
  KEY `IX_Addresses_UserId_IsDefault` (`UserId`,`IsDefault`),
  CONSTRAINT `FK_Addresses_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `addresses`
--

LOCK TABLES `addresses` WRITE;
/*!40000 ALTER TABLE `addresses` DISABLE KEYS */;
/*!40000 ALTER TABLE `addresses` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `announcements`
--

DROP TABLE IF EXISTS `announcements`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `announcements` (
  `AnnouncementId` int NOT NULL AUTO_INCREMENT,
  `Text` varchar(300) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`AnnouncementId`),
  KEY `IX_Announcements_DisplayOrder` (`DisplayOrder`),
  KEY `IX_Announcements_IsActive` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `announcements`
--

LOCK TABLES `announcements` WRITE;
/*!40000 ALTER TABLE `announcements` DISABLE KEYS */;
/*!40000 ALTER TABLE `announcements` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cartitems`
--

DROP TABLE IF EXISTS `cartitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cartitems` (
  `CartItemId` int NOT NULL AUTO_INCREMENT,
  `CartId` int NOT NULL,
  `ProductId` int NOT NULL,
  `Size` varchar(50) DEFAULT NULL,
  `Color` varchar(50) DEFAULT NULL,
  `Quantity` int NOT NULL,
  `UnitPrice` decimal(10,2) NOT NULL,
  `OriginalMRP` decimal(10,2) NOT NULL,
  `AddedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`CartItemId`),
  KEY `IX_CartItems_CartId_ProductId_Size_Color` (`CartId`,`ProductId`,`Size`,`Color`),
  KEY `IX_CartItems_ProductId` (`ProductId`),
  CONSTRAINT `FK_CartItems_Carts_CartId` FOREIGN KEY (`CartId`) REFERENCES `carts` (`CartId`) ON DELETE CASCADE,
  CONSTRAINT `FK_CartItems_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cartitems`
--

LOCK TABLES `cartitems` WRITE;
/*!40000 ALTER TABLE `cartitems` DISABLE KEYS */;
/*!40000 ALTER TABLE `cartitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `carts`
--

DROP TABLE IF EXISTS `carts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `carts` (
  `CartId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`CartId`),
  UNIQUE KEY `IX_Carts_UserId` (`UserId`),
  CONSTRAINT `FK_Carts_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `carts`
--

LOCK TABLES `carts` WRITE;
/*!40000 ALTER TABLE `carts` DISABLE KEYS */;
/*!40000 ALTER TABLE `carts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `categories`
--

DROP TABLE IF EXISTS `categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `categories` (
  `CategoryId` int NOT NULL AUTO_INCREMENT,
  `Name` longtext NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`CategoryId`),
  UNIQUE KEY `IX_Categories_CategoryId` (`CategoryId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `categories`
--

LOCK TABLES `categories` WRITE;
/*!40000 ALTER TABLE `categories` DISABLE KEYS */;
/*!40000 ALTER TABLE `categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orderitems`
--

DROP TABLE IF EXISTS `orderitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orderitems` (
  `OrderItemId` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `ProductId` int NOT NULL,
  `ProductName` varchar(200) NOT NULL,
  `ProductImage` varchar(300) DEFAULT NULL,
  `Size` varchar(50) DEFAULT NULL,
  `Color` varchar(50) DEFAULT NULL,
  `Quantity` int NOT NULL,
  `UnitPrice` decimal(10,2) NOT NULL,
  `OriginalMRP` decimal(10,2) NOT NULL,
  `LineTotal` decimal(10,2) NOT NULL,
  PRIMARY KEY (`OrderItemId`),
  KEY `IX_OrderItems_OrderId` (`OrderId`),
  KEY `IX_OrderItems_ProductId` (`ProductId`),
  CONSTRAINT `FK_OrderItems_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE,
  CONSTRAINT `FK_OrderItems_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orderitems`
--

LOCK TABLES `orderitems` WRITE;
/*!40000 ALTER TABLE `orderitems` DISABLE KEYS */;
/*!40000 ALTER TABLE `orderitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orders`
--

DROP TABLE IF EXISTS `orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orders` (
  `OrderId` int NOT NULL AUTO_INCREMENT,
  `OrderNumber` varchar(50) NOT NULL,
  `UserId` int NOT NULL,
  `Status` varchar(30) NOT NULL,
  `Subtotal` decimal(10,2) NOT NULL,
  `Discount` decimal(10,2) NOT NULL,
  `Shipping` decimal(10,2) NOT NULL,
  `Total` decimal(10,2) NOT NULL,
  `PaymentMethod` varchar(50) NOT NULL,
  `PaymentStatus` varchar(30) NOT NULL,
  `RazorpayOrderId` varchar(100) DEFAULT NULL,
  `RazorpayPaymentId` varchar(100) DEFAULT NULL,
  `RazorpaySignature` varchar(200) DEFAULT NULL,
  `PaymentAttempts` int NOT NULL,
  `LastPaymentAttemptAt` datetime(6) DEFAULT NULL,
  `FailureReason` varchar(500) DEFAULT NULL,
  `ExpiresAt` datetime(6) DEFAULT NULL,
  `PaidAt` datetime(6) DEFAULT NULL,
  `RefundId` varchar(100) DEFAULT NULL,
  `RefundedAmount` decimal(10,2) DEFAULT NULL,
  `RefundedAt` datetime(6) DEFAULT NULL,
  `ShippingFullName` varchar(100) NOT NULL,
  `ShippingPhone` varchar(20) NOT NULL,
  `ShippingLine1` varchar(200) NOT NULL,
  `ShippingLine2` varchar(200) DEFAULT NULL,
  `ShippingCity` varchar(100) NOT NULL,
  `ShippingState` varchar(100) NOT NULL,
  `ShippingPincode` varchar(10) NOT NULL,
  `ShippingCountry` varchar(100) NOT NULL,
  `CourierName` varchar(100) DEFAULT NULL,
  `TrackingNumber` varchar(100) DEFAULT NULL,
  `ShippedAt` datetime(6) DEFAULT NULL,
  `DeliveredAt` datetime(6) DEFAULT NULL,
  `DelhiveryWaybill` varchar(50) DEFAULT NULL,
  `DelhiveryStatus` varchar(50) DEFAULT NULL,
  `LastTrackingSyncAt` datetime(6) DEFAULT NULL,
  `CodFee` decimal(10,2) NOT NULL,
  `CodCollectedAt` datetime(6) DEFAULT NULL,
  `CancelledAt` datetime(6) DEFAULT NULL,
  `CancelledBy` varchar(50) DEFAULT NULL,
  `CancellationReason` varchar(300) DEFAULT NULL,
  `RefundStatus` varchar(50) DEFAULT NULL,
  `PlacedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`OrderId`),
  UNIQUE KEY `IX_Orders_OrderNumber` (`OrderNumber`),
  KEY `IX_Orders_UserId` (`UserId`),
  CONSTRAINT `FK_Orders_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orders`
--

LOCK TABLES `orders` WRITE;
/*!40000 ALTER TABLE `orders` DISABLE KEYS */;
/*!40000 ALTER TABLE `orders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `otplogs`
--

DROP TABLE IF EXISTS `otplogs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `otplogs` (
  `OtpLogId` int NOT NULL AUTO_INCREMENT,
  `Phone` varchar(20) NOT NULL,
  `OtpCode` varchar(10) NOT NULL,
  `Purpose` varchar(30) NOT NULL,
  `IsUsed` tinyint(1) NOT NULL,
  `AttemptCount` int NOT NULL,
  `ExpiresAt` datetime(6) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`OtpLogId`),
  KEY `IX_OtpLogs_ExpiresAt` (`ExpiresAt`),
  KEY `IX_OtpLogs_Phone` (`Phone`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `otplogs`
--

LOCK TABLES `otplogs` WRITE;
/*!40000 ALTER TABLE `otplogs` DISABLE KEYS */;
/*!40000 ALTER TABLE `otplogs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `paymentattempts`
--

DROP TABLE IF EXISTS `paymentattempts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `paymentattempts` (
  `PaymentAttemptId` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `RazorpayOrderId` varchar(100) DEFAULT NULL,
  `RazorpayPaymentId` varchar(100) DEFAULT NULL,
  `Status` varchar(30) NOT NULL,
  `ErrorCode` varchar(100) DEFAULT NULL,
  `ErrorDescription` varchar(500) DEFAULT NULL,
  `ErrorReason` varchar(50) DEFAULT NULL,
  `ErrorStep` varchar(50) DEFAULT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `AttemptedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`PaymentAttemptId`),
  KEY `IX_PaymentAttempts_OrderId` (`OrderId`),
  KEY `IX_PaymentAttempts_RazorpayPaymentId` (`RazorpayPaymentId`),
  CONSTRAINT `FK_PaymentAttempts_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `paymentattempts`
--

LOCK TABLES `paymentattempts` WRITE;
/*!40000 ALTER TABLE `paymentattempts` DISABLE KEYS */;
/*!40000 ALTER TABLE `paymentattempts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `products`
--

DROP TABLE IF EXISTS `products`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `products` (
  `ProductId` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) NOT NULL,
  `ShortDescription` varchar(500) DEFAULT NULL,
  `Description` longtext,
  `MRP` decimal(10,2) NOT NULL,
  `SalePrice` decimal(10,2) DEFAULT NULL,
  `SavePercent` int DEFAULT NULL,
  `CategoryId` int NOT NULL,
  `Sizes` varchar(200) DEFAULT NULL,
  `Colors` varchar(200) DEFAULT NULL,
  `Image1` varchar(300) NOT NULL,
  `Image2` varchar(300) NOT NULL,
  `Image3` varchar(300) DEFAULT NULL,
  `Image4` varchar(300) DEFAULT NULL,
  `VideoUrl` varchar(300) DEFAULT NULL,
  `SKU` varchar(100) DEFAULT NULL,
  `Brand` varchar(100) DEFAULT NULL,
  `Composition` varchar(200) DEFAULT NULL,
  `GSM` varchar(50) DEFAULT NULL,
  `PrintType` varchar(200) DEFAULT NULL,
  `Neckline` varchar(100) DEFAULT NULL,
  `FitType` varchar(100) DEFAULT NULL,
  `CountryOfProduction` varchar(100) DEFAULT NULL,
  `WashCare` longtext,
  `SizingNote` varchar(500) DEFAULT NULL,
  `MRPNote` varchar(200) DEFAULT NULL,
  `AdditionalNote` longtext,
  `Tags` varchar(1000) DEFAULT NULL,
  `Benefits` varchar(1000) DEFAULT NULL,
  `VoucherCode` varchar(100) DEFAULT NULL,
  `VoucherDiscountPercent` int DEFAULT NULL,
  `VoucherMinOrderValue` decimal(10,2) DEFAULT NULL,
  `VoucherMaxDiscount` decimal(10,2) DEFAULT NULL,
  `VoucherValidUntil` datetime(6) DEFAULT NULL,
  `VoucherIsActive` tinyint(1) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `IsFeatured` tinyint(1) NOT NULL,
  `IsNewArrival` tinyint(1) NOT NULL,
  `Stock` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WeightGrams` int NOT NULL,
  `LengthCm` int DEFAULT NULL,
  `WidthCm` int DEFAULT NULL,
  `HeightCm` int DEFAULT NULL,
  PRIMARY KEY (`ProductId`),
  UNIQUE KEY `IX_Products_ProductId` (`ProductId`),
  KEY `IX_Products_CategoryId` (`CategoryId`),
  CONSTRAINT `FK_Products_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `categories` (`CategoryId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `products`
--

LOCK TABLES `products` WRITE;
/*!40000 ALTER TABLE `products` DISABLE KEYS */;
/*!40000 ALTER TABLE `products` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `refreshtokens`
--

DROP TABLE IF EXISTS `refreshtokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `refreshtokens` (
  `TokenId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `Token` longtext NOT NULL,
  `ExpiryDate` datetime(6) NOT NULL,
  `IsRevoked` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`TokenId`),
  KEY `IX_RefreshTokens_UserId` (`UserId`),
  CONSTRAINT `FK_RefreshTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `refreshtokens`
--

LOCK TABLES `refreshtokens` WRITE;
/*!40000 ALTER TABLE `refreshtokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `refreshtokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `RoleId` int NOT NULL AUTO_INCREMENT,
  `RoleName` varchar(255) NOT NULL,
  PRIMARY KEY (`RoleId`),
  UNIQUE KEY `IX_Roles_RoleName` (`RoleName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `UserId` int NOT NULL AUTO_INCREMENT,
  `FullName` longtext NOT NULL,
  `Email` varchar(255) DEFAULT NULL,
  `Password` longtext,
  `Phone` varchar(255) DEFAULT NULL,
  `RoleId` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `IX_Users_Email` (`Email`),
  UNIQUE KEY `IX_Users_Phone` (`Phone`),
  KEY `IX_Users_RoleId` (`RoleId`),
  CONSTRAINT `FK_Users_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `roles` (`RoleId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `webhookevents`
--

DROP TABLE IF EXISTS `webhookevents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `webhookevents` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EventId` varchar(100) NOT NULL,
  `EventType` varchar(60) DEFAULT NULL,
  `RazorpayOrderId` varchar(100) DEFAULT NULL,
  `RazorpayPaymentId` varchar(100) DEFAULT NULL,
  `Payload` longtext NOT NULL,
  `Status` varchar(20) NOT NULL,
  `Attempts` int NOT NULL,
  `Error` varchar(1000) DEFAULT NULL,
  `ReceivedAt` datetime(6) NOT NULL,
  `ProcessedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_WebhookEvents_EventId` (`EventId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `webhookevents`
--

LOCK TABLES `webhookevents` WRITE;
/*!40000 ALTER TABLE `webhookevents` DISABLE KEYS */;
/*!40000 ALTER TABLE `webhookevents` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-10-12 21:52:01
