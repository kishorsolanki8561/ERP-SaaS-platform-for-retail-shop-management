CREATE OR ALTER PROCEDURE [sequence].[usp_AllocateSequenceNumber]
    @ShopId          BIGINT,
    @Code            NVARCHAR(50),
    @AllocatedNumber BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Lock the row exclusively so concurrent calls queue up rather than racing.
        UPDATE [sequence].[SequenceDefinition]
        SET    [LastNumber] = [LastNumber] + 1
        WHERE  [ShopId] = @ShopId
          AND  [Code]   = @Code;

        IF @@ROWCOUNT = 0
        BEGIN
            -- Auto-create sequence on first use starting at 1.
            INSERT INTO [sequence].[SequenceDefinition]
                   ([ShopId], [Code], [LastNumber], [PadLength],
                    [CreatedAtUtc], [IsDeleted], [RowVersion])
            VALUES (@ShopId, @Code, 1, 6,
                    GETUTCDATE(), 0, 0x);

            SET @AllocatedNumber = 1;
        END
        ELSE
        BEGIN
            SELECT @AllocatedNumber = [LastNumber]
            FROM   [sequence].[SequenceDefinition]
            WHERE  [ShopId] = @ShopId
              AND  [Code]   = @Code;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;   -- re-raise the original error to the caller
    END CATCH
END;
GO
