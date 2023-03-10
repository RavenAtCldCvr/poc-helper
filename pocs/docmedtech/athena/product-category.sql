SELECT a.*, b.*, c.*
FROM "product_categories"."pom_store_prd_table_cscart_products_categories_uzli_aj7z_table" a
         JOIN "category_listing"."pom_store_prd_table_cscart_categories_laee_oco7_table" b
              ON a.category_id = b.category_id
         JOIN "category_listing_descriptions"."pom_store_prd_table_cscart_category_descriptions_d7qv_5hn_table" c
              ON a.category_id = c.category_id
where b.level = 3
  and a.link_type = 'A'
