-- MySQL dump 10.13  Distrib 5.7.17, for Win64 (x86_64)
--
-- Host: localhost    Database: application
-- ------------------------------------------------------
-- Server version	5.7.22-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `a_answers`
--

DROP TABLE IF EXISTS `a_answers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `a_answers` (
  `a_key` int(11) NOT NULL AUTO_INCREMENT,
  `a_stars` tinyint(4) NOT NULL DEFAULT '0',
  `a_comment` varchar(1000) NOT NULL DEFAULT '',
  `a_poll_key` int(11) NOT NULL,
  `a_user_key` int(11) NOT NULL,
  PRIMARY KEY (`a_key`),
  KEY `a_poll_key` (`a_poll_key`),
  KEY `a_user_key` (`a_user_key`),
  CONSTRAINT `a_answers_ibfk_1` FOREIGN KEY (`a_poll_key`) REFERENCES `a_polls` (`p_key`),
  CONSTRAINT `a_answers_ibfk_2` FOREIGN KEY (`a_user_key`) REFERENCES `a_users_profiles` (`u_p_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `a_answers`
--

LOCK TABLES `a_answers` WRITE;
/*!40000 ALTER TABLE `a_answers` DISABLE KEYS */;
/*!40000 ALTER TABLE `a_answers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `a_polls`
--

DROP TABLE IF EXISTS `a_polls`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `a_polls` (
  `p_key` int(11) NOT NULL AUTO_INCREMENT,
  `p_title` varchar(250) NOT NULL,
  `p_description` varchar(500) NOT NULL DEFAULT '',
  `p_position` tinyint(4) NOT NULL,
  `p_user_key` int(11) NOT NULL,
  PRIMARY KEY (`p_key`),
  KEY `p_user_key` (`p_user_key`),
  CONSTRAINT `a_polls_ibfk_1` FOREIGN KEY (`p_user_key`) REFERENCES `a_users_profiles` (`u_p_key`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `a_polls`
--

LOCK TABLES `a_polls` WRITE;
/*!40000 ALTER TABLE `a_polls` DISABLE KEYS */;
INSERT INTO `a_polls` VALUES (1,'Prueba','prueba',1,2);
/*!40000 ALTER TABLE `a_polls` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `a_users`
--

DROP TABLE IF EXISTS `a_users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `a_users` (
  `u_key` int(11) NOT NULL AUTO_INCREMENT,
  `u_email` varchar(50) NOT NULL,
  `u_password` varchar(500) NOT NULL,
  PRIMARY KEY (`u_key`),
  UNIQUE KEY `u_email` (`u_email`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `a_users`
--

LOCK TABLES `a_users` WRITE;
/*!40000 ALTER TABLE `a_users` DISABLE KEYS */;
INSERT INTO `a_users` VALUES (2,'c@c.c','b1b3773a05c0ed0176787a4f1574ff0075f7521e'),(4,'a@a.a','7c4a8d09ca3762af61e59520943dc26494f8941b');
/*!40000 ALTER TABLE `a_users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `a_users_profiles`
--

DROP TABLE IF EXISTS `a_users_profiles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `a_users_profiles` (
  `u_p_key` int(11) NOT NULL,
  `u_p_user_name` varchar(25) NOT NULL,
  `u_p_name` varchar(50) NOT NULL,
  PRIMARY KEY (`u_p_key`),
  UNIQUE KEY `u_p_user` (`u_p_user_name`),
  CONSTRAINT `a_users_profiles_ibfk_1` FOREIGN KEY (`u_p_key`) REFERENCES `a_users` (`u_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `a_users_profiles`
--

LOCK TABLES `a_users_profiles` WRITE;
/*!40000 ALTER TABLE `a_users_profiles` DISABLE KEYS */;
INSERT INTO `a_users_profiles` VALUES (2,'c','C'),(4,'usuario4','Usuario4');
/*!40000 ALTER TABLE `a_users_profiles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'application'
--

--
-- Dumping routines for database 'application'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2019-04-20 13:30:41
