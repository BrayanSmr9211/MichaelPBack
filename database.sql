-- ============================================================
--  GestionTareas - Script de Base de Datos SQL Server
--  Servidor: DESKTOP-PELKD5V\MSSQLSERVER2
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GestionTareas')
BEGIN
    CREATE DATABASE GestionTareas;
END
GO

USE GestionTareas;
GO

-- ============================================================
--  Tabla: Users
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id            INT            IDENTITY(1,1) NOT NULL,
        Nombre        NVARCHAR(100)  NOT NULL,
        Email         NVARCHAR(200)  NOT NULL,
        FechaCreacion DATETIME2      NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Users PRIMARY KEY (Id),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );

    -- Indice unico sobre Email para busquedas rapidas y garantia de unicidad
    CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
END
GO

-- ============================================================
--  Tabla: Tasks
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tasks')
BEGIN
    CREATE TABLE Tasks (
        Id                INT            IDENTITY(1,1) NOT NULL,
        Titulo            NVARCHAR(200)  NOT NULL,
        Descripcion       NVARCHAR(1000) NULL,
        Estado            NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
        UsuarioId         INT            NOT NULL,
        FechaCreacion     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        FechaActualizacion DATETIME2     NULL,

        -- Columna JSON para informacion adicional
        -- Ejemplo: {"prioridad":"Alta","fechaEstimada":"2024-12-31","etiquetas":["backend","api"],"metadata":{}}
        InfoAdicional     NVARCHAR(MAX)  NULL
            CONSTRAINT CK_Tasks_InfoAdicional_IsJson
                CHECK (InfoAdicional IS NULL OR ISJSON(InfoAdicional) = 1),

        CONSTRAINT PK_Tasks PRIMARY KEY (Id),
        CONSTRAINT FK_Tasks_Users FOREIGN KEY (UsuarioId)
            REFERENCES Users (Id)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT CK_Tasks_Estado
            CHECK (Estado IN ('Pending', 'InProgress', 'Done'))
    );

    -- Indice en UsuarioId para filtros por usuario
    CREATE INDEX IX_Tasks_UsuarioId ON Tasks (UsuarioId);

    -- Indice en Estado para filtros por estado
    CREATE INDEX IX_Tasks_Estado ON Tasks (Estado);

    -- Indice compuesto para la consulta mas comun: usuario + estado ordenado por fecha
    CREATE INDEX IX_Tasks_UsuarioId_Estado_FechaCreacion
        ON Tasks (UsuarioId, Estado, FechaCreacion);
END
GO

-- ============================================================
--  Datos de prueba
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Users)
BEGIN
    INSERT INTO Users (Nombre, Email, FechaCreacion) VALUES
        ('Carlos Perez',    'carlos.perez@empresa.com',  GETUTCDATE()),
        ('Maria Lopez',     'maria.lopez@empresa.com',   GETUTCDATE()),
        ('Juan Rodriguez',  'juan.rodriguez@empresa.com',GETUTCDATE());

    INSERT INTO Tasks (Titulo, Descripcion, Estado, UsuarioId, FechaCreacion, InfoAdicional) VALUES
        ('Implementar login',   'Crear endpoint de autenticacion JWT',  'Pending',
         1, GETUTCDATE(),
         N'{"prioridad":"Alta","fechaEstimada":"2024-12-15","etiquetas":["backend","seguridad"],"metadata":{"sprint":1}}'),

        ('Disenar base de datos','Crear modelo relacional en SQL Server','InProgress',
         2, GETUTCDATE(),
         N'{"prioridad":"Alta","fechaEstimada":"2024-12-10","etiquetas":["database","sql"],"metadata":{"sprint":1}}'),

        ('Crear frontend Angular','Modulo de listado de tareas con filtros','Pending',
         3, GETUTCDATE(),
         N'{"prioridad":"Media","fechaEstimada":"2024-12-20","etiquetas":["frontend","angular"],"metadata":{"sprint":2}}'),

        ('Escribir unit tests',  'Cobertura minima del 80%',             'Done',
         1, DATEADD(DAY,-2,GETUTCDATE()),
         N'{"prioridad":"Baja","fechaEstimada":"2024-12-05","etiquetas":["testing"],"metadata":{"sprint":1}}');
END
GO

-- ============================================================
--  CONSULTAS DE DEMOSTACION - MANEJO DE JSON EN SQL SERVER
-- ============================================================

-- 1. ISJSON: Validar que InfoAdicional sea JSON valido
SELECT
    Id,
    Titulo,
    InfoAdicional,
    ISJSON(InfoAdicional) AS EsJsonValido
FROM Tasks
WHERE InfoAdicional IS NOT NULL;
GO

-- 2. JSON_VALUE: Leer un campo escalar del JSON (prioridad)
SELECT
    t.Id,
    t.Titulo,
    t.Estado,
    u.Nombre AS UsuarioNombre,
    JSON_VALUE(t.InfoAdicional, '$.prioridad')      AS Prioridad,
    JSON_VALUE(t.InfoAdicional, '$.fechaEstimada')  AS FechaEstimada
FROM Tasks t
INNER JOIN Users u ON t.UsuarioId = u.Id
WHERE t.InfoAdicional IS NOT NULL;
GO

-- 3. JSON_QUERY: Leer un objeto/array del JSON (etiquetas)
SELECT
    Id,
    Titulo,
    JSON_QUERY(InfoAdicional, '$.etiquetas')  AS Etiquetas,
    JSON_QUERY(InfoAdicional, '$.metadata')   AS Metadata
FROM Tasks
WHERE InfoAdicional IS NOT NULL;
GO

-- 4. Filtrar tareas por valor dentro del JSON (solo prioridad "Alta")
SELECT
    t.Id,
    t.Titulo,
    t.Estado,
    u.Nombre AS UsuarioNombre,
    JSON_VALUE(t.InfoAdicional, '$.prioridad') AS Prioridad
FROM Tasks t
INNER JOIN Users u ON t.UsuarioId = u.Id
WHERE JSON_VALUE(t.InfoAdicional, '$.prioridad') = 'Alta';
GO

-- 5. OPENJSON: Descomponer el JSON en filas (expandir etiquetas)
SELECT
    t.Id,
    t.Titulo,
    etiqueta.value AS Etiqueta
FROM Tasks t
CROSS APPLY OPENJSON(t.InfoAdicional, '$.etiquetas') AS etiqueta
WHERE t.InfoAdicional IS NOT NULL;
GO

-- 6. (Opcional) Actualizar un campo especifico dentro del JSON
UPDATE Tasks
SET InfoAdicional = JSON_MODIFY(InfoAdicional, '$.prioridad', 'Critica')
WHERE Id = 1
  AND ISJSON(InfoAdicional) = 1;
GO

-- 7. Consulta completa: tareas por usuario, filtro por estado, orden por fecha de creacion
SELECT
    t.Id,
    t.Titulo,
    t.Descripcion,
    t.Estado,
    u.Id   AS UsuarioId,
    u.Nombre AS UsuarioNombre,
    u.Email  AS UsuarioEmail,
    t.FechaCreacion,
    t.FechaActualizacion,
    JSON_VALUE(t.InfoAdicional, '$.prioridad')     AS Prioridad,
    JSON_VALUE(t.InfoAdicional, '$.fechaEstimada') AS FechaEstimada,
    JSON_QUERY(t.InfoAdicional, '$.etiquetas')     AS Etiquetas
FROM Tasks t
INNER JOIN Users u ON t.UsuarioId = u.Id
WHERE
    (@UsuarioId IS NULL OR t.UsuarioId = @UsuarioId)     -- filtro por usuario
    AND (@Estado IS NULL OR t.Estado = @Estado)           -- filtro por estado
ORDER BY t.FechaCreacion ASC;
GO
