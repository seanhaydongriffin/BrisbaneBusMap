using GMap.NET;
using GMap.NET.MapProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProtoBuf;
using TransitRealtime;
using System.IO;
using System.Data.SQLite;
using System.Collections;
using System.Net;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsForms.ToolTips;

namespace GoogleMap
{
    public partial class Form1 : Form
    {
        private SQLiteConnection cnn = null;
        private SQLiteCommand cmd = null;
        private List<string[]> route_id = null;
        private PointLatLng next_position = new PointLatLng();
        private GMarkerGoogle marker = null;
        private GMarkerGoogle[] markers = new GMarkerGoogle[20];

        public Form1()
        {
            InitializeComponent();
            //       webBrowser1.Navigate("http://maps.google.com");


            //            gmap.MapProvider = GMap.NET.MapProviders.BingMapProvider.Instance;
            //gmap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            //GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            //gmap.SetPositionByKeywords("Paris, France");



            //try
            //{
            //    System.Net.IPHostEntry e =
            //         System.Net.Dns.GetHostEntry("www.google.com");
            //}
            //catch
            //{
            //    MainMap.Manager.Mode = AccessMode.CacheOnly;
            //    MessageBox.Show("No internet connection avaible, going to CacheOnly mode.",
            //          "GMap.NET - Demo.WindowsForms", MessageBoxButtons.OK,
            //          MessageBoxIcon.Warning);
            //}

            //// config map
            gmap.MapProvider = GMapProviders.GoogleMap; //.OpenStreetMap;
            gmap.Position = new PointLatLng(-27.6316159, 153.0503168);
            gmap.MinZoom = 2;
            gmap.MaxZoom = 18;
            gmap.Zoom = 13;
            //    gmap.SetPositionByKeywords("Paris, France");

            var overlay1 = new GMapOverlay("OverlayOne");
            MarkerTooltipMode mode = MarkerTooltipMode.Always;

            for (int marker_num = 0; marker_num < 20; marker_num++)
            {
                markers[marker_num] = new GMarkerGoogle(new PointLatLng(0, 0), GMarkerGoogleType.green);
                markers[marker_num].ToolTip = new GMapBaloonToolTip(markers[marker_num]);
                markers[marker_num].ToolTipMode = mode;
                Brush ToolTipBackColor = new SolidBrush(Color.Transparent);
                markers[marker_num].ToolTip.Fill = ToolTipBackColor;
                markers[marker_num].ToolTipText = "";
                overlay1.Markers.Add(markers[marker_num]);
            }

            gmap.Overlays.Add(overlay1);



            // using System.Net;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Use SecurityProtocolType.Ssl3 if needed for compatibility reasons


            //            string connStr = @"URI=file:R:\BUS\BUS\BrisbaneBusConsole\SEQ_GTFS.db";
            string connStr = @"URI=file:R:\BUS\BUS\GoogleMap\fred2.db";

            cnn = new SQLiteConnection(connStr);
            
            cnn.Open();
            cnn.EnableExtensions(true);
            cnn.LoadExtension("mod_spatialite");
            cmd = cnn.CreateCommand();
            cmd.CommandText = "SELECT InitSpatialMetaData(1);";
            cmd.ExecuteScalar();
            

//            route_id = SQLite.ToList("SELECT route_id FROM routes where route_short_name = '130';", cnn);
            route_id = SQLite.ToList("SELECT route_id FROM routes where route_short_name = 'GLKN';", cnn);



            UpdateGMap(null, null);


            Timer timer = new Timer();
//            timer.Interval = (10 * 1000); // 10 secs
            timer.Interval = (5 * 1000); // 10 secs
            timer.Tick += new EventHandler(UpdateGMap);
            timer.Start();


//            cnn.Close();
  //          cnn.Dispose();




        }

        private void UpdateGMap(object sender, EventArgs e)
        {
            bool found_position = false;

            try
            {

                WebRequest req = HttpWebRequest.Create("https://gtfsrt.api.translink.com.au/Feed/SEQ");
                FeedMessage feed = Serializer.Deserialize<FeedMessage>(req.GetResponse().GetResponseStream());
                List<object[]> bus_positions = new List<object[]>();

                foreach (FeedEntity entity in feed.Entities)
                {
                    if (found_position)

                        break;

                    if (entity.Vehicle != null)
                    {
              //          Console.WriteLine("Route ID = " + entity.Vehicle.Trip.RouteId);

                        for (int route_num = 0; route_num < route_id.Count; route_num++)
                        {
                            if (entity.Vehicle.Trip.RouteId.Equals(route_id[route_num][0]))
                            {

                                Console.WriteLine("Trip ID = " + entity.Vehicle.Trip.TripId);
                                Console.WriteLine("Route ID = " + entity.Vehicle.Trip.RouteId);
                                Console.WriteLine("Vehicle ID = " + entity.Vehicle.Vehicle.Id);
                                Console.WriteLine("Vehicle Label = " + entity.Vehicle.Vehicle.Label);
                                Console.WriteLine("Vehicle License Plate = " + entity.Vehicle.Vehicle.LicensePlate);
                                Console.WriteLine("Vehicle Stop ID = " + entity.Vehicle.StopId);
                                Console.WriteLine("Vehicle Timestamp = " + entity.Vehicle.Timestamp);
                                Console.WriteLine("Vehicle Latitude = " + entity.Vehicle.Position.Latitude);
                                Console.WriteLine("Vehicle Longitude = " + entity.Vehicle.Position.Longitude);

                                cmd.CommandText = "SELECT Distance(GeomFromText('POINT(153.0503168 -27.6316159)',4326),GeomFromText('POINT(" + entity.Vehicle.Position.Longitude + " " + entity.Vehicle.Position.Latitude + ")',4326), 0) FROM routes;";  //set the passed query
                                var result = cmd.ExecuteScalar().ToString();
                                Console.WriteLine("Vehicle Distance = " + result + " metres");
                                double distance = Convert.ToDouble(result);

                                cmd.CommandText = "SELECT stop_name FROM stops WHERE stop_id = '" + entity.Vehicle.StopId + "';";  //set the passed query
                                var stop_name = cmd.ExecuteScalar().ToString();


                                object[] bus_data = { (double)entity.Vehicle.Position.Latitude, (double)entity.Vehicle.Position.Longitude, (double)distance, stop_name };
                                bus_positions.Add(bus_data);



//                                marker.Position = new PointLatLng(entity.Vehicle.Position.Latitude, entity.Vehicle.Position.Longitude);
  //                              gmap.Position = new PointLatLng(entity.Vehicle.Position.Latitude, entity.Vehicle.Position.Longitude);


                                //      found_position = true;
                                //     break;
                            }

                        }
                    }
                }

                Console.WriteLine("Number of 130 buses = " + bus_positions.Count);

                for (int marker_num = 0; marker_num < 20; marker_num++)
                
                    markers[marker_num].Position = new PointLatLng(0, 0);

                for (int bus_num = 0; bus_num < bus_positions.Count && bus_num < 20; bus_num++)
                {
                    markers[bus_num].Position = new PointLatLng((double)bus_positions[bus_num][0], (double)bus_positions[bus_num][1]);
                    markers[bus_num].ToolTipText = (string)bus_positions[bus_num][3];
                }

                int i = 0;
            }
            catch (Exception e2)
            {

            }

        }
    }
}
