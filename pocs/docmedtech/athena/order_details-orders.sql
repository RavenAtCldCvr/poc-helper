SELECT a.product_id, product_code, price, amount, b.*
FROM "AwsDataCatalog"."order_details"."pom_store_prd_table_cscart_order_details_bwij_j5q1_table" a
         LEFT JOIN "AwsDataCatalog"."pharma_orders"."catalog_pharma_orders_zipcode" b
                   on a.order_id = b.order_id
where b.order_id = 12767
   or b.parent_order_id = 12767
order by b.order_id limit 1000
