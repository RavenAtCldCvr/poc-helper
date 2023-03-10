﻿/* This is a autogenerated query from Data Pipes */
SELECT a.order_id,
       a.product_id,
       a.product_code,
       a.price,
       a.amount,
       b.is_parent_order,
       b.parent_order_id,
       b.total,
       b.subtotal,
       b.discount,
       b.subtotal_discount,
       b.firstname,
       c.category_id,
       c.link_type,
       c.position,
       c.category_position,
       d.id_path,
       d.level,
       d.status,
       d.product_count,
       d.position,
       d.is_op,
       d.localization,
       d.age_verification,
       d.age_limit,
       d.parent_age_verification,
       d.parent_age_limit,
       d.selected_views,
       d.default_view,
       d.product_details_view,
       d.product_columns,
       d.is_trash,
       d.is_default,
       d.category_type,
       d.is_featured,
       e.lang_code,
       e.category,
       e.description,
       e.meta_keywords,
       e.meta_description,
       e.page_title,
       e.age_warning_message,
       f.b_country,
       f.b_state,
       f.b_city,
       f.b_zipcode,
       f.s_country,
       f.s_state,
       f.s_city,
       f.s_zipcode,
       h.Latitude   as "b_latitude",
       h.longtitude as "b_longtitude",
       i.Latitude   as "s_latitude",
       i.longtitude as "s_longtitude",
       g.user_type
FROM "AwsDataCatalog"."order_details"."pom_store_prd_table_cscart_order_details_bwij_j5q1_table" a
         LEFT JOIN "AwsDataCatalog"."pharma_orders"."orders_with_vendor" b
                   on a.order_id = b.order_id
         JOIN "product_categories"."pom_store_prd_table_cscart_products_categories_uzli_aj7z_table" c
              on a.product_id = c.product_id
         JOIN "category_listing"."pom_store_prd_table_cscart_categories_laee_oco7_table" d
              ON c.category_id = d.category_id
         JOIN "category_listing_descriptions"."pom_store_prd_table_cscart_category_descriptions_d7qv_5hn_table" e
              ON d.category_id = e.category_id
         JOIN "user_profiles"."pom_store_prd_table_cscart_user_profiles_k9sx_nxj7_table" f
              ON b.user_id = f.user_id
         JOIN "pharma_users"."pom_store_prd_table_cscart_users_9jyn_l0cw_table" g
              ON b.user_id = g.user_id
         LEFT JOIN "postal_codes"."postal_code_dedup" h
                   on
                       f.b_zipcode = h.postal
         LEFT JOIN "postal_codes"."postal_code_dedup" i
                   on
                       f.s_zipcode = i.postal
where
/* category filtering, reduce most duplicates */
    d.level = 1
  and c.link_type = 'A'
/* user filtering */
  and g.user_type = 'C'
/* order filtering
and (b.order_id = 12767 or b.parent_order_id = 12767) */
  and b.is_parent_order = 'N'
  and b.parent_order_id!=0 and a.price > 0
order by product_id

/*
How is parent_order used
1 Parent order -> N Orders
1 order -> N order details
1 order detail -> N Products
but in this query the total amount for
CG003L order + FLU12T order + LORO5T order, does not add up to 952.3 in the parent's total
but order details price * qty for all orders, sums up to subtotal in the parent order subtotal.
parent order's total price appears to be subtotal * 1.07 for GST

Hence, the aggregate of the subtotals of all child order is used for reporting in the dashboard.
*/

/*
for Parent order_id = 12767
appears to have duplicate records for orders, some with price = 0, some have similar category, i.e. DERMA and DEMATOLOGY
*/

/*
for all joined orders, i.e remove all filters, there doesn't seem to have be records where user type is vendor
*/