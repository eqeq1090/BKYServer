CREATE DATABASE  IF NOT EXISTS `vill_global_master` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `vill_global_master`;
-- MySQL dump 10.13  Distrib 8.0.32, for Win64 (x86_64)
--
-- Host: localhost    Database: vill_global_master
-- ------------------------------------------------------
-- Server version	8.0.19

--
-- Table structure for table `email_list`
--

DROP TABLE IF EXISTS `email_list`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `email_list` (
  `email` varchar(128) NOT NULL,
  `status` tinyint NOT NULL DEFAULT '1',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `group_num` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `login_history`
--

DROP TABLE IF EXISTS `login_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `login_history` (
  `player_uid` bigint NOT NULL,
  `email` varchar(128) NOT NULL,
  `reg_date` datetime NOT NULL,
  `behavior_type` tinyint NOT NULL DEFAULT '0',
  `group_num` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`player_uid`,`email`,`reg_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `maintenance_info`
--

DROP TABLE IF EXISTS `maintenance_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `maintenance_info` (
  `status` tinyint NOT NULL,
  `hashcode` varchar(48) NOT NULL DEFAULT 'no_hash_code',
  PRIMARY KEY (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `bk_global_player`
--

DROP TABLE IF EXISTS `bk_global_player`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bk_global_player` (
  `player_uid` bigint NOT NULL AUTO_INCREMENT,
  `did` varchar(128) NOT NULL,
  `hive_player_id` bigint NOT NULL,
  `access_token` varchar(128) NOT NULL,
  `email` varchar(128) NOT NULL DEFAULT '',
  `shard_num` int NOT NULL,
  `name` varchar(45) DEFAULT NULL,
  `is_blocked` tinyint NOT NULL DEFAULT '0',
  `player_tag` varchar(8) NOT NULL DEFAULT 'tag',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `login_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`),
  UNIQUE KEY `access_token_email_UNIQUE` (`access_token`,`player_uid`,`email`)
) ENGINE=InnoDB AUTO_INCREMENT=437 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

-- Dump completed on 2023-06-13 13:55:28
