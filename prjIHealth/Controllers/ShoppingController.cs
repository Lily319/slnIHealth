﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjiHealth.Models;
using prjiHealth.ViewModels;
using prjIHealth.Models;
using prjIHealth.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace prjiHealth.Controllers
{
    public class ShoppingController : Controller
    {
        private readonly IHealthContext dblIHealth;
        public ShoppingController(IHealthContext db)
        {
            dblIHealth = db;
        }

        public IActionResult ShoppingCartList()
        {
            if (HttpContext.Session.Keys.Contains(CDictionary.SK_Shopped_Items))
            {
                string jsonCart = HttpContext.Session.GetString(CDictionary.SK_Shopped_Items);
                List<CShoppingCartItem> cart = JsonSerializer.Deserialize<List<CShoppingCartItem>>(jsonCart);
                return View(cart);
            }
            //待有資料後改回以下兩行
            else
                return RedirectToAction("ShowShoppingMall");
            //return View();
        }

        public IActionResult CheckOut()
        {
            return View();
        }

        //商城主頁界面
        public IActionResult ShowShoppingMall()
        {
            return View();
        }

        int userID = 0;
        public void TakeMemberID(CLoginViewModel vModel)
        {
            var q = dblIHealth.TMembers.FirstOrDefault(tm => tm.FUserName == vModel.fUserName);
            string loginSession = JsonSerializer.Serialize(q);
            HttpContext.Session.SetString(CDictionary.SK_Logined_User, loginSession);
            TMember loginUser = JsonSerializer.Deserialize<TMember>(loginSession);
            userID = loginUser.FMemberId;
        }

        //商城搜尋功能
        public IActionResult SearchProduct(string keyword)
        {
            IEnumerable<TProduct> dataShoppingItems = null;
            if (!string.IsNullOrEmpty(keyword))
            {
                dataShoppingItems = from t in dblIHealth.TProducts
                                    where t.FProductName.Contains(keyword)
                                    select t;
            }
            return Json(dataShoppingItems);
        }

        //在商城主頁顯示各個產品
        public IActionResult ShowProduct(int? id)
        {
            IEnumerable<TProduct> dataShoppingItems = null;
            //TODO           
            if (id == null)
            {
                dataShoppingItems = from t in dblIHealth.TProducts
                                    select t;
            }
            else
            {
                dataShoppingItems = from t in dblIHealth.TProducts
                                    where t.FCategoryId == id
                                    select t;
            }
            return Json(dataShoppingItems);
        }

        //傳入產品、會員ID 把商品加入到資料庫當中
        public IActionResult AddToTrack(int? id)
        {
            var p = new CProductViewModel().ProductList.FirstOrDefault(t => t.FProductId == id);

            //判定產品有沒有曾出現於該會員的追蹤清單內
            var q = (from a in dblIHealth.TTrackLists
                     where a.FMemberId == 1 && a.FProductId == id
                     select a).Count();

            if (q != 0)
            {
                return Content("你已加入追蹤清單");
            }
            else
            {
                if (p != null)
                {
                    TTrackList trackList = new TTrackList()
                    {
                        FMemberId = 1/*userID*/,
                        //TODO GET member id
                        FProductId = Convert.ToInt32(id)
                    };
                    dblIHealth.TTrackLists.Add(trackList);
                    dblIHealth.SaveChanges();
                }
                return Json(p);
            }
        }

        //價格由大到小排序
        public IActionResult DescProduct(int? id)
        {
            IEnumerable<TProduct> dataShoppingItems = null;
            if (id == null || id == 0)
            {
                dataShoppingItems = from t in dblIHealth.TProducts
                                    orderby t.FUnitprice descending
                                    select t;
            }
            else
            {
                dataShoppingItems = from t in dblIHealth.TProducts
                                    where t.FCategoryId == id
                                    orderby t.FUnitprice descending
                                    select t;
            }
            return Json(dataShoppingItems);
        }

        //價格由小到大排序
        public IActionResult AscProduct(int? id)
        {
            IEnumerable<TProduct> dataShoppingItems = null;
            if (id == null || id == 0)
            {
                dataShoppingItems = from t in dblIHealth.TProducts
                                    orderby t.FUnitprice ascending
                                    select t;
            }
            else
            {
                dataShoppingItems = from t in dblIHealth.TProducts
                                    where t.FCategoryId == id
                                    orderby t.FUnitprice ascending
                                    select t;
            }
            return Json(dataShoppingItems);
        }

        //產品明細界面
        public ActionResult ShowProductDetail(int? id)
        {
            //var prod = new CProductViewModel().ProductList.FirstOrDefault(t => t.FProductId == id);
            //var profinclude = dblIHealth.TProducts.Include(p => p.TProductsImages).Where(t => t.FProductId == id).AsEnumerable();
            TProduct prod = dblIHealth.TProducts.FirstOrDefault(t => t.FProductId == id);
            if (prod == null)
            {
                return RedirectToAction("ShowShoppingMall");
            }
            return View(prod);
        }

        [HttpPost]
        public ActionResult ShowProductDetail(CAddToCartViewModel vModel)
        {
            IHealthContext db = new IHealthContext();
            //TDiscount discount = db.TDiscounts.FirstOrDefault(t => t.FDiscountCode == vModel.discountCode);
            TProduct prod = db.TProducts.FirstOrDefault(t => t.FProductId == vModel.txtFid);
            if (prod == null)
            {
                return RedirectToAction("ShowShoppingMall");
            }
            string jsonCart = "";
            List<CShoppingCartItem> list = null;
            if (!HttpContext.Session.Keys.Contains(CDictionary.SK_Shopped_Items))
            {
                list = new List<CShoppingCartItem>();
            }
            else
            {
                jsonCart = HttpContext.Session.GetString(CDictionary.SK_Shopped_Items);
                list = JsonSerializer.Deserialize<List<CShoppingCartItem>>(jsonCart);
            }

            CShoppingCartItem item = new CShoppingCartItem()
            {
                count = vModel.txtCount,
                //discount=Convert.ToDecimal(discount.FDiscountValue),
                price = (decimal)prod.FUnitprice,
                productId = vModel.txtFid,
                product = prod
            };

            if (list.Count == 0)
            {
                list.Add(item);
            }
            else
            {
                bool sameproduct = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].productId == item.productId)
                    {
                        list[i].count += item.count;
                        sameproduct = true;
                    }
                }
                if (!sameproduct)
                {
                    list.Add(item);
                }
            }
            jsonCart = JsonSerializer.Serialize(list);
            HttpContext.Session.SetString(CDictionary.SK_Shopped_Items, jsonCart);
            return RedirectToAction("ShowShoppingMall");
        }

        //以ProductID搜對應的圖片
        public ActionResult ShowProductImages(int? id)
        {
            var images = from p in dblIHealth.TProducts
                         join i in dblIHealth.TProductsImages
                         on p.FProductId equals i.FProductId
                         where p.FProductId == id
                         select i.FImage;
            return Json(images);
        }

        //顯示各類別前3名圖片
        public ActionResult ShowTop3Product(int? id)
        {
            // var top3PopularProduct = null;
            ArrayList list = new ArrayList();
            var top3PopularProduct = (from od in dblIHealth.TOrderDetails.Include(od => od.FProduct).ThenInclude(p => p.FCategory).AsEnumerable()
                                      where od.FProduct.FCategoryId == id
                                      group od by new
                                      {
                                          od.FProductId,
                                          od.FProduct.FProductName,
                                          od.FProduct.FCoverImage,
                                          od.FProduct.FUnitprice
                                      }
                                      into g
                                      select new
                                      {
                                          Key = g.Key, 
                                          Count = g.Sum(od => od.FQuantity),
                                          Photo = g.Key.FCoverImage,
                                          UnitPrice = g.Key.FUnitprice
                                      }).OrderByDescending(p => p.Count).Take(3).ToList();

            list.Add(top3PopularProduct);

            return Json(list);
        }
        public ActionResult SuggestProduct()
        {
            ArrayList list = new ArrayList();
            Random rd = new Random(Guid.NewGuid().GetHashCode());
            int count = dblIHealth.TProducts.Count();
            int num = rd.Next(1, count);

            for (int i = 0; i < 4; i++)
            {
                TProduct rdProduct = (from t in dblIHealth.TProducts
                                      where t.FProductId == num
                                      select t).FirstOrDefault();
                list.Add(rdProduct);
            }
            return Json(list);
        }
    }
}
