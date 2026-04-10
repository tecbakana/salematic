-- Migration: adiciona campos de endereço na tabela Clientes
-- Executar no SalematicDB

ALTER TABLE Clientes
    ADD Cep         VARCHAR(8)    NULL,
        Logradouro  NVARCHAR(300) NULL,
        Numero      VARCHAR(20)   NULL,
        Complemento NVARCHAR(100) NULL,
        Bairro      NVARCHAR(100) NULL,
        Cidade      NVARCHAR(100) NULL,
        Estado      CHAR(2)       NULL;
GOfei
