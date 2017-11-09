using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
namespace vlc_x_callback {
    class xCallbackUrl {
        const string xCallbackUrlValue = @"x-callback-url";
        readonly Uri _uri;
        public xCallbackUrl( System.Uri uri ) {
            if( uri == null )
                throw new ArgumentNullException( nameof( uri ), @"uri parameter not optional" );
            if( !xCallbackUrlValue.Equals( uri.Authority, StringComparison.InvariantCulture ) )
                throw new ArgumentException( $@"Authority needs to be {xCallbackUrlValue} and not {uri.Authority}" );
            _uri = uri;
        }
        string _action;
        public string Action {
            get {
                return _action ?? ( _action = _uri.AbsolutePath.TrimStart( '/' ) );
            }
        }
        public string Scheme {
            get {
                return _uri.Scheme;
            }
        }
        NameValueCollection _query;
        NameValueCollection Query {
            get {
                return _query ?? ( _query = HttpUtility.ParseQueryString( _uri.Query ) );
            }
        }
        public string[] Parameters {
            get {
                return Query.AllKeys;
            }
        }
        public IEnumerable<KeyValuePair<string, string>> ParameterValues {
            get {
                foreach( var k in Query.AllKeys )
                    yield return new KeyValuePair<string, string>( k, Query[ k ] );
            }
        }
        string _commandLine;
        public string CommandLine {
            get {
                return _commandLine ?? ( _commandLine = BuildCommandLine() );
            }
        }
        private string BuildCommandLine() {
            var sb = new StringBuilder( 512 );
            sb.Append( Scheme );
            sb.Append( ' ' );
            sb.Append( Action );
            foreach( var p in ParameterValues ) {
                sb.Append( ' ' );
                sb.Append( '-' );
                sb.Append( p.Key );
                sb.Append( ' ' );
                sb.Append( p.Value );
            }
            return sb.ToString();
        }
        public string this[ string parameter ] {
            get {
                return Query[ parameter ];
            }
        }
        public static void Register( string protocol, string defaultIcon, FileInfo executeable ) {
            var k = Registry.ClassesRoot.CreateSubKey(protocol);
            k.CreateSubKey( @"DefaultIcon" ).SetValue( string.Empty, defaultIcon );
            k.CreateSubKey( @"shell" ).CreateSubKey( @"open" ).CreateSubKey( @"command" ).SetValue( string.Empty, $@"""{executeable.FullName}"" ""%1""" );
            k.SetValue( string.Empty, $@"URL:{protocol} Protocol" );
            k.SetValue( @"URL Protocol", string.Empty );
        }
        public static void Unregister( string protocol ) {
            Registry.ClassesRoot.DeleteSubKeyTree( protocol );
        }
    }
    class Program {
        const string Protocol = @"vlc-x-callback";
        static string VlcPlayer = Environment.ExpandEnvironmentVariables( @"%ProgramW6432%\VideoLAN\VLC\VLC.exe" );
        static void Main( string[] args ) {
            var parameter = args.FirstOrDefault();
            if( @"-register".Equals( parameter, StringComparison.InvariantCultureIgnoreCase ) )
                xCallbackUrl.Register(
                    protocol: Protocol,
                    defaultIcon: $@"{VlcPlayer},0",
                    executeable: new FileInfo( new Program().GetType().GetTypeInfo().Assembly.Location )
                );
            else if( @"-unregister".Equals( parameter, StringComparison.InvariantCultureIgnoreCase ) )
                xCallbackUrl.Unregister(
                    protocol: Protocol
                );
            else if( @"-help".Equals( parameter, StringComparison.InvariantCultureIgnoreCase ) || @"-?".Equals( parameter ) )
                Console.WriteLine( $@"Usage: {Protocol} -register | -unregister | x-callback-url" );
            else
                foreach( var arg in args ) {
                    var xc = new xCallbackUrl(new Uri(arg));
                    Process.Start( new ProcessStartInfo( VlcPlayer, xc[ @"url" ] ) {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    } );
                }
        }
    }
}
