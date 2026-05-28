ALTER TABLE Clientes
    ADD ResetPasswordToken NVARCHAR(100) NULL,
        ResetPasswordExpiry DATETIME2 NULL;
