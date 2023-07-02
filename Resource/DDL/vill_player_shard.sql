-- MySQL dump 10.13  Distrib 8.0.32, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: vill_player_shard
-- ------------------------------------------------------
-- Server version	8.0.32

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
-- Table structure for table `vill_daily_shop_slot`
--

DROP TABLE IF EXISTS `vill_daily_shop_slot`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_daily_shop_slot` (
  `player_uid` bigint NOT NULL,
  `daily_shop_slot` int NOT NULL,
  `daily_shop_master_id` int NOT NULL,
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`daily_shop_slot`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_frame`
--

DROP TABLE IF EXISTS `vill_frame`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_frame` (
  `player_uid` bigint NOT NULL,
  `frame_master_id` int NOT NULL,
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`frame_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_killmarker`
--

DROP TABLE IF EXISTS `vill_killmarker`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_killmarker` (
  `player_uid` bigint NOT NULL,
  `killmarker_master_id` int NOT NULL DEFAULT '0',
  `reg_date` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`killmarker_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_mission`
--

DROP TABLE IF EXISTS `vill_mission`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_mission` (
  `mission_id` bigint NOT NULL AUTO_INCREMENT,
  `player_uid` bigint NOT NULL,
  `mission_master_id` int NOT NULL,
  `state` int NOT NULL DEFAULT '0',
  `current_count` int NOT NULL DEFAULT '0',
  `client_checked` tinyint NOT NULL DEFAULT '0',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`mission_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_battle_log`
--

DROP TABLE IF EXISTS `vill_player_battle_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_battle_log` (
	`id` BIGINT(19) NOT NULL AUTO_INCREMENT,
	`player_uid` BIGINT(19) NOT NULL,
	`game_mode` SMALLINT(5) NOT NULL,
	`region_code` SMALLINT(5) NOT NULL,
	`room_id` BIGINT(19) NOT NULL,
	`rank` INT(10) NOT NULL,
	`pilot_master_id` INT(10) NOT NULL,
	`robot_master_id` INT(10) NOT NULL,
	`trophy` INT(10) NOT NULL,
	`mmr` INT(10) NOT NULL,
	`reg_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`id`) USING BTREE,
	INDEX `index_player_uid` (`player_uid`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_currency`
--

DROP TABLE IF EXISTS `vill_player_currency`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_currency` (
  `player_uid` bigint NOT NULL,
  `currency_type` int NOT NULL,
  `currency_amount` bigint NOT NULL DEFAULT '0',
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `next_recharge_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`currency_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_emoticon`
--

DROP TABLE IF EXISTS `vill_player_emoticon`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_emoticon` (
  `player_uid` bigint NOT NULL,
  `emoticon_master_id` int NOT NULL,
  `slot_mask_sum` int NOT NULL DEFAULT '0',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`emoticon_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_friend`
--

DROP TABLE IF EXISTS `vill_player_friend`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_friend` (
  `player_uid` bigint NOT NULL,
  `friend_player_uid` bigint NOT NULL,
  `is_deleted` tinyint NOT NULL DEFAULT '0',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`friend_player_uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_friend_recommends`
--

DROP TABLE IF EXISTS `vill_player_friend_recommends`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_friend_recommends` (
  `player_uid` bigint NOT NULL,
  `friend_player_uid` bigint NOT NULL,
  `is_deleted` tinyint NOT NULL DEFAULT '0',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`friend_player_uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_friend_requests`
--

DROP TABLE IF EXISTS `vill_player_friend_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_friend_requests` (
  `player_uid` bigint NOT NULL,
  `friend_player_uid` bigint NOT NULL,
  `is_deleted` tinyint NOT NULL DEFAULT '0',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`friend_player_uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_gameplay_log`
--

DROP TABLE IF EXISTS `vill_player_gameplay_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_gameplay_log` (
  `player_uid` bigint NOT NULL,
  `game_mode` smallint NOT NULL,
  `play_count` int NOT NULL DEFAULT '0',
  `winning_count` int NOT NULL DEFAULT '0',
  `kill_count` int NOT NULL DEFAULT '0',
  `trophy_count` int NOT NULL DEFAULT '0',
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`game_mode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_pilot`
--

DROP TABLE IF EXISTS `vill_player_pilot`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_pilot` (
  `player_uid` bigint NOT NULL,
  `pilot_master_id` int NOT NULL,
  `level` int NOT NULL DEFAULT '1',
  `exp` int NOT NULL DEFAULT '0',
  `skin_master_id` int NOT NULL DEFAULT '0',
  `frame_master_id` int NOT NULL DEFAULT '0',
  `pose_master_id` int NOT NULL DEFAULT '0',
  `trophy_count` int NOT NULL DEFAULT '0',
  `mmr` int NOT NULL DEFAULT '0',
  `reward_step` int NOT NULL DEFAULT '0',
  `game_play_count` int NOT NULL DEFAULT '0',
  `winning_count` int NOT NULL DEFAULT '0',
  `kill_count` int NOT NULL DEFAULT '0',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`pilot_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_player_season`
--

DROP TABLE IF EXISTS `vill_player_season`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_player_season` (
  `player_uid` bigint NOT NULL,
  `season_master_id` int NOT NULL,
  `is_premium_season` tinyint NOT NULL DEFAULT '0',
  `season_reward_point` int NOT NULL DEFAULT '0',
  `season_reward_step_free` int NOT NULL DEFAULT '0',
  `season_reward_step_premium` int NOT NULL DEFAULT '0',
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`season_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_players`
--

DROP TABLE IF EXISTS `vill_players`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_players` (
  `player_uid` bigint NOT NULL,
  `name` varchar(45) NOT NULL DEFAULT 'noname',
  `profile_icon_master_id` int NOT NULL DEFAULT '0',
  `pilot_master_id` int NOT NULL DEFAULT '0',
  `robot_master_id` int NOT NULL DEFAULT '0',
  `killmarker_master_id` int NOT NULL DEFAULT '0',
  `frame_master_id` int NOT NULL DEFAULT '0',
  `tutorial_step` smallint NOT NULL DEFAULT '0',
  `region_code` smallint NOT NULL DEFAULT '0',
  `player_reward_step` int NOT NULL DEFAULT '0',
  `is_blocked` tinyint NOT NULL DEFAULT '0',
  `trophy_cnt` int NOT NULL DEFAULT '0',
  `player_tag` varchar(8) NOT NULL DEFAULT 'tag',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_pose`
--

DROP TABLE IF EXISTS `vill_pose`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_pose` (
  `player_uid` bigint NOT NULL,
  `pose_master_id` int NOT NULL,
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`pose_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_profile_icon`
--

DROP TABLE IF EXISTS `vill_profile_icon`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_profile_icon` (
  `player_uid` bigint NOT NULL,
  `profile_icon_master_id` int NOT NULL,
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`profile_icon_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_purchase_history`
--

DROP TABLE IF EXISTS `vill_purchase_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_purchase_history` (
  `purchase_id` bigint NOT NULL AUTO_INCREMENT,
  `player_uid` bigint NOT NULL,
  `shop_master_id` int NOT NULL,
  `shop_type` tinyint NOT NULL DEFAULT '0',
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`purchase_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_robot`
--

DROP TABLE IF EXISTS `vill_robot`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_robot` (
  `player_uid` bigint NOT NULL,
  `robot_master_id` int NOT NULL,
  `skin_master_id` int NOT NULL,
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `upd_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`robot_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vill_skin`
--

DROP TABLE IF EXISTS `vill_skin`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vill_skin` (
  `player_uid` bigint NOT NULL,
  `skin_master_id` int NOT NULL,
  `reg_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_uid`,`skin_master_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping routines for database 'vill_player_shard'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2023-04-11 11:57:41
