-- Script de inicialização do banco de dados
-- Cria os schemas separados para cada microserviço

-- Criar database para vendas
SELECT 'CREATE DATABASE vendasdb'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'vendasdb')\gexec

-- Criar database para inventário/estoque
SELECT 'CREATE DATABASE inventario'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'inventario')\gexec

-- Conectar ao database vendasdb para criar estruturas específicas
\c vendasdb;

-- Criar extensões necessárias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Conectar ao database inventario para criar estruturas específicas
\c inventario;

-- Criar extensões necessárias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Voltar ao database padrão
\c ecommerce;