using Android.App;
using Android.Widget;
using Android.OS;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using System;
using RestSharp;
using System.Threading.Tasks;
using System.Collections.Generic;
using GoogleMaps.Models;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Drawing;
using static Android.Gms.Maps.GoogleMap;
using Android.Gms.Common;
using Android.Util;
using Android.Content;

namespace GoogleMaps
{
    [Activity(Label = "GoogleMaps", Theme="@style/MyTheme")]
    public class MainActivity : Activity, IOnMapReadyCallback
    {
        ProgressBar progressBar;
        LinearLayout container;
        TextView polygon;

        private GoogleMap mMap;
        
        private Polygon polygone;
        private PolygonOptions rectangle;
        
        private List<List<double>> arrayPointX = new List<List<double>>();
        private List<List<double>> arrayPointY = new List<List<double>>();
        private int polygonCount = 0;
        const string TAG = "MyFirebaseIIDService";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            container = FindViewById<LinearLayout>(Resource.Id.container);
            polygon = FindViewById<TextView>(Resource.Id.polygon);

            container.RemoveView(progressBar);
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            dialog_start dialogStart = new dialog_start();
            dialogStart.Show(transaction, "dialog_fragment");
            arrayPointX.Add(new List<double>());
            arrayPointY.Add(new List<double>());
            SetUpMap();

            if (Intent.Extras != null)
            {
                foreach (var key in Intent.Extras.KeySet())
                {
                    var value = Intent.Extras.GetString(key);
                    Log.Debug(TAG, "Key: {0} Value: {1}", key, value);
                }
            }
            IsPlayServicesAvailable();
        }

        public bool IsPlayServicesAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                    Console.Write(GoogleApiAvailability.Instance.GetErrorString(resultCode));
                else
                {
                    Console.Write("Sorry, this device is not supported");
                    Finish();
                }
                return false;
            }
            else
            {
                Console.Write("Google Play Services is available.");
                return true;
            }
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            mMap = googleMap;

            mMap.MapLongClick += MMap_MapLongClick;
            mMap.MapClick += mMap_MapClick;
            mMap.MarkerClick += mMap_MarkerClick;
        }
        //Долгое нажатие
        private void MMap_MapLongClick(object sender, MapLongClickEventArgs e)
        {
            LatLng pos = e.Point;
            double minPos = 10000;
            double gip;
            int iPos = 0;
            Vibrator vibrator = (Vibrator)GetSystemService(Context.VibratorService);
            vibrator.Vibrate(500);
            for (int i = 0; i < arrayPointX.Count; i++)
            {
                for(int j = 0; j < arrayPointX[i].Count; j++)
                {
                    gip = Math.Sqrt(Math.Pow(arrayPointX[i][j] - pos.Longitude, 2) + Math.Pow(arrayPointY[i][j] - pos.Latitude, 2));
                    if (minPos > gip)
                    {
                        minPos = gip;
                        iPos = i;
                    }
                }
            }
            container.RemoveView(polygon);
            container.AddView(progressBar);
            getJSON(iPos);
        }

        private void mMap_MapClick(object sender, MapClickEventArgs e)
        {
            LatLng latLng = e.Point;
            MarkerOptions options = new MarkerOptions()
                .SetPosition(latLng)
                .InvokeIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.dot));
            mMap.AddMarker(options);
            arrayPointX[polygonCount].Add(latLng.Latitude);
            arrayPointY[polygonCount].Add(latLng.Longitude);
        }
        //1 клик по маркеру
        private void mMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            LatLng pos = e.Marker.Position;
            if (arrayPointX[polygonCount].Count > 2 && pos.Latitude == arrayPointX[polygonCount][0] && pos.Longitude == arrayPointY[polygonCount][0])
            {
                rectangle = new PolygonOptions()
                .InvokeFillColor(Color.ForestGreen.ToArgb())
                .InvokeZIndex(10);
                for (int i = 0; i < arrayPointX[polygonCount].Count; i++)
                    rectangle.Add(new LatLng(arrayPointX[polygonCount][i], arrayPointY[polygonCount][i]));
                polygone = mMap.AddPolygon(rectangle);
                polygone.StrokeWidth = 2;
                polygonCount++;
                arrayPointX.Add(new List<double>());
                arrayPointY.Add(new List<double>());
                Toast.MakeText(this, "Зажмите на области поля", ToastLength.Long).Show();
            }
        }

        private void SetUpMap()
        {
            if(mMap == null)
            {
                FragmentManager.FindFragmentById<MapFragment>(Resource.Id.map).GetMapAsync(this);
            }
        }

        private async void getJSON(int iPos)
        {
            string url = "http://evmmap.azurewebsites.net/api/getweather?pol=" + arrayPointX[iPos][0].ToString().Replace(',','.') + ";" + arrayPointY[iPos][0].ToString().Replace(',', '.');
            for (int i = 1; i < arrayPointX[iPos].Count; i++)
            {
                url += ";" + arrayPointX[iPos][i].ToString().Replace(',', '.') + ";" + arrayPointY[iPos][i].ToString().Replace(',', '.');
            }
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(url);
                var response = await client.GetAsync(client.BaseAddress);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                JObject o = JObject.Parse(content);
                var Weather = JsonConvert.DeserializeObject<WeatherModel>(o.ToString());

                container.RemoveView(progressBar);
                polygon = new TextView(this);
                polygon.Text = "Поле № " + (iPos + 1) + "\nКоордината Х: " + Weather.coord.lat.ToString() + "\nКоордината Y: " + Weather.coord.lon.ToString()
                    + "\nТемпература: " + Weather.main.temp.ToString() + " градусов" + "\nДавление: " + Weather.main.pressure.ToString() + "\nВлажность: " + Weather.main.humidity.ToString()
                    + "\nОблачность: " + Weather.clouds.all.ToString() + "\nТип: " + Weather.weather[0].main + "\nОписание: " + Weather.weather[0].description
                    + "\nСкорость ветра: " + Weather.wind.speed.ToString() + " м/с" + "\nНаправление ветра: " + Weather.wind.deg.ToString() + " градусов";
                container.AddView(polygon);
                
            }
            catch(Exception ex)
            {
                Console.Write("test");
            }
        }
    }
}

