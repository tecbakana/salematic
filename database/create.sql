-- Salematic — Script de criação do banco de dados
-- Executar no SQL Server local (SalematicDB)

CREATE DATABASE SalematicDB;
GO
USE SalematicDB;
GO

CREATE TABLE Clientes (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Nome        NVARCHAR(200) NOT NULL,
    Documento   VARCHAR(20) NOT NULL UNIQUE, -- CPF ou CNPJ
    Email       VARCHAR(200) NOT NULL,
    Telefone    VARCHAR(20) NULL
);

CREATE TABLE Produtos (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Nome            NVARCHAR(200) NOT NULL,
    Descricao       NVARCHAR(500) NULL,
    Preco           DECIMAL(10,2) NOT NULL,
    UnidadeMedida   VARCHAR(20) NOT NULL DEFAULT 'un',
    Ativo           BIT NOT NULL DEFAULT 1
);

CREATE TABLE Estoques (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    ProdutoId           INT NOT NULL REFERENCES Produtos(Id),
    Quantidade          INT NOT NULL DEFAULT 0,
    UltimaAtualizacao   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Pedidos (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId   INT NOT NULL REFERENCES Clientes(Id),
    DataPedido  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status      VARCHAR(50) NOT NULL DEFAULT 'aguardando_pagamento',
    ValorTotal  DECIMAL(10,2) NOT NULL
);

CREATE TABLE ItensPedido (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    PedidoId        INT NOT NULL REFERENCES Pedidos(Id),
    ProdutoId       INT NOT NULL REFERENCES Produtos(Id),
    NomeProduto     NVARCHAR(200) NOT NULL,
    Quantidade      INT NOT NULL,
    PrecoUnitario   DECIMAL(10,2) NOT NULL
);

-- Dados de exemplo
INSERT INTO Clientes (Nome, Documento, Email, Telefone) VALUES
('João Silva', '123.456.789-00', 'joao@email.com', '(11) 99999-0001'),
('Empresa ABC', '12.345.678/0001-90', 'contato@abc.com', '(11) 3333-0001');

INSERT INTO Produtos (Nome, Descricao, Preco, UnidadeMedida) VALUES
('Teclado Mecânico', 'Teclado mecânico RGB switch blue', 299.90, 'un'),
('Mouse Gamer', 'Mouse 16000 DPI com iluminação', 189.90, 'un'),
('Monitor 27"', 'Monitor Full HD 144Hz', 1299.00, 'un'),
('Headset', 'Headset stereo com microfone', 149.90, 'un'),
('Cabo HDMI 2m', 'Cabo HDMI 2.0 4K', 39.90, 'un');

INSERT INTO Estoques (ProdutoId, Quantidade) VALUES
(1, 15), (2, 30), (3, 8), (4, 20), (5, 50);
GO
