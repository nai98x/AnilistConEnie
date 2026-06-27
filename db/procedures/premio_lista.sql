-- Todos los premios de temporada. Las columnas se alias-ean a los nombres de la entidad (year/order).
CREATE OR REPLACE FUNCTION premio_lista()
RETURNS TABLE(nombre text, link text, "year" integer, "order" integer)
LANGUAGE sql
STABLE
AS $$
    SELECT nombre, link, anio, orden FROM premios;
$$;
