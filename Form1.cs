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
        private List<string[]> route_stops = null;
        private List<string[]> bus_next_stop_sequence = null;
        private List<string[]> my_stop_sequence = null;
        private PointLatLng next_position = new PointLatLng();
        private GMarkerGoogle marker = null;
        private GMarkerGoogle[] bus_markers = new GMarkerGoogle[20];
        private GMarkerGoogle[] stop_markers = new GMarkerGoogle[50];
        private string my_route_short_name = "130";
        private int my_stop_id = 10763;
        private int my_stop_sequence_int = -1;

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
                bus_markers[marker_num] = new GMarkerGoogle(new PointLatLng(0, 0), GMarkerGoogleType.green);
                bus_markers[marker_num].ToolTip = new GMapBaloonToolTip(bus_markers[marker_num]);
                bus_markers[marker_num].ToolTipMode = mode;
                Brush ToolTipBackColor = new SolidBrush(Color.Transparent);
                bus_markers[marker_num].ToolTip.Fill = ToolTipBackColor;
                bus_markers[marker_num].ToolTipText = "";
                overlay1.Markers.Add(bus_markers[marker_num]);
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
            

            route_id = SQLite.ToList("SELECT route_id FROM routes where route_short_name = '" + my_route_short_name + "';", cnn);
            //            route_id = SQLite.ToList("SELECT route_id FROM routes where route_short_name = 'GLKN';", cnn);



            my_stop_sequence = SQLite.ToList("SELECT stop_times.stop_sequence FROM stop_times where stop_times.stop_id = '" + my_stop_id + "' and stop_times.trip_id = '13976771-BT 19_20-BOX-Saturday-01';", cnn);
            my_stop_sequence_int = Convert.ToInt32(my_stop_sequence[0][0]);


            route_stops = SQLite.ToList("SELECT stop_times.stop_id, stop_times.stop_sequence, stops.stop_lat, stops.stop_lon, stops.stop_name FROM stop_times, stops where stop_times.stop_id = stops.stop_id and stop_times.trip_id = '13976771-BT 19_20-BOX-Saturday-01' order by CAST(stop_sequence AS INTEGER);", cnn);

            for (int marker_num = 0; marker_num < route_stops.Count; marker_num++)
            {
                stop_markers[marker_num] = new GMarkerGoogle(new PointLatLng(Convert.ToDouble(route_stops[marker_num][2]), Convert.ToDouble(route_stops[marker_num][3])), GMarkerGoogleType.red_small);
                stop_markers[marker_num].ToolTip = new GMapBaloonToolTip(stop_markers[marker_num]);
                stop_markers[marker_num].ToolTipMode = mode;
                Brush ToolTipBackColor = new SolidBrush(Color.Transparent);
                stop_markers[marker_num].ToolTip.Fill = ToolTipBackColor;
                stop_markers[marker_num].ToolTipText = route_stops[marker_num][0];
                overlay1.Markers.Add(stop_markers[marker_num]);
            }


            // Make a small copy of the route we are interested in

            //cmd.CommandText = "DROP VIEW IF EXISTS `my_stop_times`;";
            //cmd.ExecuteNonQuery();
            //cmd.CommandText = "CREATE VIEW `my_stop_times` AS SELECT * FROM stop_times WHERE trip_id = '13976771-BT 19_20-BOX-Saturday-01';";
            //cmd.ExecuteNonQuery();
            cmd.CommandText = "DROP TABLE IF EXISTS `my_stop_times`;";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE `my_stop_times` ( `trip_id` , `arrival_time` , `departure_time` , `stop_id` , `stop_sequence` , `pickup_type` , `drop_off_type` );";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO my_stop_times SELECT * FROM stop_times WHERE trip_id = '13976771-BT 19_20-BOX-Saturday-01';";
            cmd.ExecuteNonQuery();





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


//                                bus_next_stop_sequence = SQLite.ToList("SELECT stop_times.stop_sequence FROM stop_times where stop_times.stop_id = '" + entity.Vehicle.StopId + "' and stop_times.trip_id = '13976771-BT 19_20-BOX-Saturday-01';", cnn);
                                bus_next_stop_sequence = SQLite.ToList("SELECT my_stop_times.stop_sequence FROM my_stop_times where my_stop_times.stop_id = '" + entity.Vehicle.StopId + "' and my_stop_times.trip_id = '13976771-BT 19_20-BOX-Saturday-01';", cnn);
                                int bus_next_stop_sequence_int = -1;

                                if (bus_next_stop_sequence.Count > 0)

                                    bus_next_stop_sequence_int = Convert.ToInt32(bus_next_stop_sequence[0][0]);

                                if (bus_next_stop_sequence_int > -1 && bus_next_stop_sequence_int <= my_stop_sequence_int)
                                {


                                    Console.WriteLine("Trip ID = " + entity.Vehicle.Trip.TripId);
                                    Console.WriteLine("Route ID = " + entity.Vehicle.Trip.RouteId);
                                    Console.WriteLine("Vehicle ID = " + entity.Vehicle.Vehicle.Id);
                                    Console.WriteLine("Vehicle Label = " + entity.Vehicle.Vehicle.Label);
                                    Console.WriteLine("Vehicle License Plate = " + entity.Vehicle.Vehicle.LicensePlate);
                                    Console.WriteLine("Vehicle Stop ID = " + entity.Vehicle.StopId);
                                    Console.WriteLine("Vehicle Timestamp = " + entity.Vehicle.Timestamp);
                                    Console.WriteLine("Vehicle Speed = " + entity.Vehicle.Position.Speed);
                                    Console.WriteLine("Vehicle Bearing = " + entity.Vehicle.Position.Bearing);
                                    Console.WriteLine("Vehicle Odometer = " + entity.Vehicle.Position.Odometer);
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
                                }


//                                marker.Position = new PointLatLng(entity.Vehicle.Position.Latitude, entity.Vehicle.Position.Longitude);
  //                              gmap.Position = new PointLatLng(entity.Vehicle.Position.Latitude, entity.Vehicle.Position.Longitude);


                                //      found_position = true;
                                //     break;
                            }

                        }
                    }
                }

                Console.WriteLine("Number of " + my_route_short_name + " buses = " + bus_positions.Count);

                for (int marker_num = 0; marker_num < 20; marker_num++)

                    bus_markers[marker_num].Position = new PointLatLng(0, 0);

                for (int bus_num = 0; bus_num < bus_positions.Count && bus_num < 20; bus_num++)
                {
                    bus_markers[bus_num].Position = new PointLatLng((double)bus_positions[bus_num][0], (double)bus_positions[bus_num][1]);
                    bus_markers[bus_num].ToolTipText = (string)bus_positions[bus_num][3];
                }

                int i = 0;
            }
            catch (Exception e2)
            {

            }

        }
    }
}
