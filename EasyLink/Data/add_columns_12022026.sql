-- Adding new columns to the ShopItem table
ALTER TABLE ShopItems
    -- Positive integer, non-nullable, defaults to 0
    -- Using UNSIGNED to ensure the value is positive
    ADD COLUMN Amount INT NOT NULL DEFAULT 0,

    -- String column with a limit of 350 characters
    ADD COLUMN Content VARCHAR(350);