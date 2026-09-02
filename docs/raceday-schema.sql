-- =====================================================
-- RaceDay Database Schema
-- Run this script on a fresh SQL Server instance in SSMS
-- =====================================================

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- =====================================================
-- Table: Users
-- =====================================================
CREATE TABLE Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL CHECK (role IN ('Organiser','Participant')),
    phone_number VARCHAR(20) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- =====================================================
-- Table: Events
-- =====================================================
CREATE TABLE Events (
    event_id INT IDENTITY(1,1) PRIMARY KEY,
    organiser_id INT NOT NULL,
    event_name VARCHAR(150) NOT NULL,
    description VARCHAR(1000) NULL,
    event_date DATE NOT NULL,
    event_type VARCHAR(20) NOT NULL CHECK (event_type IN ('Run','Walk','Cycle')),
    status VARCHAR(20) NOT NULL DEFAULT 'Upcoming' CHECK (status IN ('Upcoming','Completed','Cancelled')),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (organiser_id) REFERENCES Users(user_id)
);
GO

-- =====================================================
-- Table: Venues (1:1 with Events)
-- =====================================================
CREATE TABLE Venues (
    venue_id INT IDENTITY(1,1) PRIMARY KEY,
    event_id INT NOT NULL UNIQUE,
    venue_name VARCHAR(150) NOT NULL,
    address VARCHAR(255) NOT NULL,
    city VARCHAR(100) NOT NULL,
    latitude DECIMAL(9,6) NULL,
    longitude DECIMAL(9,6) NULL,
    CONSTRAINT FK_Venues_Event FOREIGN KEY (event_id) REFERENCES Events(event_id)
);
GO

-- =====================================================
-- Table: Categories
-- =====================================================
CREATE TABLE Categories (
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    event_id INT NOT NULL,
    category_name VARCHAR(100) NOT NULL,
    distance_km DECIMAL(5,2) NOT NULL,
    price DECIMAL(8,2) NOT NULL DEFAULT 0,
    max_participants INT NOT NULL DEFAULT 100,
    CONSTRAINT FK_Categories_Event FOREIGN KEY (event_id) REFERENCES Events(event_id)
);
GO

-- =====================================================
-- Table: Enrolments
-- =====================================================
CREATE TABLE Enrolments (
    enrolment_id INT IDENTITY(1,1) PRIMARY KEY,
    participant_id INT NOT NULL,
    category_id INT NOT NULL,
    enrolment_date DATETIME NOT NULL DEFAULT GETDATE(),
    bib_number VARCHAR(10) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Confirmed' CHECK (status IN ('Confirmed','Cancelled')),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (participant_id) REFERENCES Users(user_id),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (category_id) REFERENCES Categories(category_id),
    CONSTRAINT UQ_Enrolment_Once UNIQUE (participant_id, category_id)
);
GO

-- =====================================================
-- Table: Results (1:1 with Enrolments)
-- =====================================================
CREATE TABLE Results (
    result_id INT IDENTITY(1,1) PRIMARY KEY,
    enrolment_id INT NOT NULL UNIQUE,
    finish_time TIME NULL,
    position INT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Finished' CHECK (status IN ('Finished','DNF','DNS')),
    recorded_at DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (enrolment_id) REFERENCES Enrolments(enrolment_id)
);
GO

-- =====================================================
-- SEED DATA
-- =====================================================

-- 2 Organisers, 2 Participants (minimum required)
INSERT INTO Users (full_name, email, password_hash, role, phone_number) VALUES
('Sarah Naidoo', 'sarah.naidoo@raceday.co.za', 'HASHED_PASSWORD_1', 'Organiser', '0821234567'),
('Thabo Mokoena', 'thabo.mokoena@raceday.co.za', 'HASHED_PASSWORD_2', 'Organiser', '0837654321'),
('Lindiwe Zulu', 'lindiwe.zulu@gmail.com', 'HASHED_PASSWORD_3', 'Participant', '0725551234'),
('Johan van der Merwe', 'johan.vdm@gmail.com', 'HASHED_PASSWORD_4', 'Participant', '0736669876');
GO

-- 3 Events (minimum required)
INSERT INTO Events (organiser_id, event_name, description, event_date, event_type, status) VALUES
(1, 'Pretoria Park Run Challenge', 'A community 5km and 10km run through Pretoria city parks.', '2026-11-14', 'Run', 'Upcoming'),
(1, 'Tshwane Charity Walk', 'Charity walk raising funds for local schools.', '2026-11-21', 'Walk', 'Upcoming'),
(2, 'Highveld Cycle Classic', 'A scenic cycling event through the Highveld region.', '2026-12-05', 'Cycle', 'Upcoming');
GO

-- Venues (1 per event)
INSERT INTO Venues (event_id, venue_name, address, city, latitude, longitude) VALUES
(1, 'Union Buildings Gardens', '1 Government Ave, Arcadia', 'Pretoria', -25.744186, 28.211477),
(2, 'Church Square', 'Church St, Pretoria Central', 'Pretoria', -25.747000, 28.187900),
(3, 'Rietvlei Nature Reserve', 'Game Reserve Ave', 'Centurion', -25.885000, 28.264000);
GO

-- Categories (at least one per event)
INSERT INTO Categories (event_id, category_name, distance_km, price, max_participants) VALUES
(1, '5km Fun Run', 5.00, 100.00, 300),
(1, '10km Run', 10.00, 150.00, 300),
(2, '5km Charity Walk', 5.00, 50.00, 500),
(3, '40km Road Cycle', 40.00, 250.00, 200),
(3, '80km Road Cycle', 80.00, 350.00, 150);
GO

-- Sample enrolments
INSERT INTO Enrolments (participant_id, category_id, bib_number, status) VALUES
(3, 2, 'B1001', 'Confirmed'),
(4, 1, 'B1002', 'Confirmed'),
(3, 4, 'B2001', 'Confirmed'),
(4, 5, 'B2002', 'Confirmed');
GO

-- Sample results
INSERT INTO Results (enrolment_id, finish_time, position, status) VALUES
(1, '00:52:14', 5, 'Finished'),
(2, '00:24:30', 12, 'Finished');
GO
Add author comment to SQL script