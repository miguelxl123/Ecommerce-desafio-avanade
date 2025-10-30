-- Script para criar tabelas e dados do microserviço de estoque
-- Execute no banco: inventario

-- Criar tabela Products
CREATE TABLE IF NOT EXISTS "Products" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000),
    "Price" numeric(18,2) NOT NULL,
    "StockQuantity" integer NOT NULL,
    "Category" character varying(50),
    "Sku" character varying(20),
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Products" PRIMARY KEY ("Id")
);

-- Criar índices
CREATE INDEX IF NOT EXISTS "IX_Products_Category" ON "Products" ("Category");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Products_Sku" ON "Products" ("Sku");
CREATE INDEX IF NOT EXISTS "IX_Products_Name" ON "Products" ("Name");
CREATE INDEX IF NOT EXISTS "IX_Products_IsActive" ON "Products" ("IsActive");

-- Inserir dados de exemplo
INSERT INTO "Products" ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "Price", "Sku", "StockQuantity", "UpdatedAt") 
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Eletrônicos', NOW(), 'Smartphone top de linha com 256GB de armazenamento', true, 'Smartphone Samsung Galaxy S23', 2499.99, 'SAMS23-256', 50, null),
    ('22222222-2222-2222-2222-222222222222', 'Informática', NOW(), 'Notebook profissional com Intel i7 e 16GB RAM', true, 'Notebook Lenovo ThinkPad', 4299.99, 'LEN-TP-I7', 25, null),
    ('33333333-3333-3333-3333-333333333333', 'Áudio', NOW(), 'Fone com cancelamento de ruído ativo', true, 'Fone de Ouvido Sony WH-1000XM5', 1599.99, 'SONY-WH1000', 100, null),
    ('44444444-4444-4444-4444-444444444444', 'TV e Home Theater', NOW(), 'Smart TV com resolução 4K e HDR', true, 'Smart TV 55" 4K Samsung', 3299.99, 'SAMS-TV55-4K', 15, null),
    ('55555555-5555-5555-5555-555555555555', 'Games', NOW(), 'Console de videogame de última geração', true, 'Console PlayStation 5', 4499.99, 'SONY-PS5', 8, null)
ON CONFLICT ("Id") DO NOTHING;

-- Criar tabela de migrations do Entity Framework
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Inserir registro da migration
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20251030000929_InitialCreate', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;