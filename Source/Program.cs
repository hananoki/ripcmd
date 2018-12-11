
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using AppCommon;

namespace ripcmd {
	public class Element {
		public string path;
		public string title;
		public int position;
		public int no;
	}

	public struct MatchFunc {
		public string meta;
		public Action<GroupCollection> func;
		public MatchFunc( string m, Action<GroupCollection> func ) {
			this.meta = m;
			this.func = func;
		}
	}

	public class App {
		List<Element> m_list = new List<Element>();
		int m_sampleRate;
		string m_performer = "PERFORMER";
		string m_title = "TITLE";


		public int run2( string[] args ) {
			string text = args[ 1 ];
			if( text.isPathRooted() ) {
				rt.setCurrentDir( text.GetDirectory() );
			}

			m_title = text.GetBaseName();
			for( int k = 2; k < args.Length; k++ ) {
				args[ k ].match( "PERFORMER=(.*)", m => {
					m_performer = m[ 1 ].Value;
				} );
			}

			using( var st = new StreamReader( text, EncodeHelper.Shift_JIS ) ) {
				while( st.Peek() >= 0 ) {
					var s = st.ReadLine();
					var array = new MatchFunc[ 4 ];
					array[ 0 ] = new MatchFunc( "SamplesPerSec=([0-9]+)", m => {
						m_sampleRate = m[ 1 ].Value.toInt32();
					} );
					array[ 1 ] = new MatchFunc( "\\[Element\\]", m => {
						m_list.Add( new Element() );
					} );
					array[ 2 ] = new MatchFunc( "Path=(.+)", m => {
						string bname = m[ 1 ].Value.GetBaseName();
						bname.match( "[0-9][0-9] (.*)", mm => {
							bname = mm[ 1 ].Value;
						} );
						m_list[ m_list.Count - 1 ].title = bname;
					} );
					array[ 3 ] = new MatchFunc( "Position=([0-9]+)", m => {
						m_list[ m_list.Count - 1 ].position = m[ 1 ].Value.toInt32();
					} );

					var array2 = array;
					var array3 = array2;
					for( int j = 0; j < array3.Length; j++ ) {
						var matchFunc = array3[ j ];
						if( s.match( matchFunc.meta, matchFunc.func ) ) {
							break;
						}
					}
				}
			}

			m_list.Sort( ( a, b ) => a.position - b.position );
			using( var st = new StreamWriter( text.changeExt( "cue" ), false, EncodeHelper.Shift_JIS ) ) {
				st.WriteLine( $"PERFORMER {m_performer.quote()}" );
				st.WriteLine( $"TITLE {m_title.quote()}" );
				st.WriteLine( "FILE {0} WAVE".format( text.getFileName().changeExt( "wav" ).quote() ) );

				foreach( var current in m_list.Select( ( v, i ) => new { v, i } ) ) {
					int num = current.i + 1;
					st.WriteLine( "  TRACK {0:00} AUDIO".format( num ) );
					st.WriteLine( "    TITLE {0}".format( current.v.title.quote() ) );
					int num2 = current.v.position / m_sampleRate;
					int num3 = current.v.position % m_sampleRate;
					double num4 = Math.Floor( num2 % 60.0 );
					double num5 = Math.Floor( num2 / 60.0 );
					float num6 = num3 / (float) m_sampleRate;
					int num7 = (int) ( num6 * 75f );
					st.WriteLine( "    INDEX 01 {0:00}:{1:00}:{2:00}".format( num5, num4, num7 ) );
				}
			}
			return 0;
		}


		public int run3( string[] args ) {
			string text = args[ 1 ];
			var list = new List<string>();
			if( text.isPathRooted() ) {
				rt.setCurrentDir( text.GetDirectory() );
			}
			var indexLst = new List<Element>();
			using( var st = new StreamReader( text, EncodeHelper.Shift_JIS ) ) {
				while( st.Peek() >= 0 ) {
					string s3 = st.ReadLine();
					var array = new MatchFunc[ 3 ];
					array[ 0 ] = new MatchFunc( "\\[Element\\]", m => {
						indexLst.Add( new Element() );
					} );
					array[ 1 ] = new MatchFunc( "Path=(.+)", m => {
						indexLst[ indexLst.Count - 1 ].path = m[ 1 ].Value;
					} );
					array[ 2 ] = new MatchFunc( "Position=([0-9]+)", m => {
						indexLst[ indexLst.Count - 1 ].position = m[ 1 ].Value.toInt32();
					} );
					var array2 = array;
					var array3 = array2;
					for( int i = 0; i < array3.Length; i++ ) {
						var matchFunc = array3[ i ];
						if( s3.match( matchFunc.meta, matchFunc.func ) ) {
							break;
						}
					}
				}
			}

			indexLst.Sort( ( a, b ) => a.position - b.position );
			using( var st2 = new StreamReader( text, EncodeHelper.Shift_JIS ) ) {
				while( st2.Peek() >= 0 ) {
					string s = st2.ReadLine();
					MatchFunc[] array4 = {
						new MatchFunc("Path=(.+)",  m=> {
							int num = indexLst.FindIndex((Element item) => item.path == m[1].Value);
							if (num < 0)							{
								throw new Exception();
							}
							string path = indexLst[num].path;
							string bname = m[1].Value.GetBaseName();
							bname.match("[0-9][0-9] (.*)", delegate(GroupCollection mm)
							{
								bname = mm[1].Value;
							});
							string fileName = m[1].Value.getFileName();
							string text2 = "{0:00} {1}".format(new object[]
							{
								num + 1,
								bname
							});
							string s2 = "Path={0}\\{1}".format(new object[]
							{
								text2,
								fileName
							});
							if (!Directory.Exists(text2))
							{
								Directory.CreateDirectory(text2);
							}
							if (File.Exists(m[1].Value))
							{
								if (path.isPathRooted())
								{
									File.Copy(m[1].Value, text2 + "\\" + fileName);
								}
								else
								{
									string text3 = text2 + "\\" + fileName;
									if (text3 != m[1].Value)
									{
										if (File.Exists(text3))
										{
											File.Delete(text3);
										}
										File.Move(m[1].Value, text3);
									}
								}
							}
							s = s2;
						})
					};
					MatchFunc[] array5 = array4;
					for( int j = 0; j < array5.Length; j++ ) {
						MatchFunc matchFunc2 = array5[ j ];
						if( s.match( matchFunc2.meta, matchFunc2.func ) ) {
							break;
						}
					}
					list.Add( s );
				}
			}

			try {
				File.Move( text, text + ".bak" );
			}
			catch( Exception ex ) {
				Debug.Log( ex.ToString(), new object[ 0 ] );
			}
			using( StreamWriter streamWriter = new StreamWriter( text, false, EncodeHelper.Shift_JIS ) ) {
				foreach( string current in list ) {
					streamWriter.WriteLine( current );
				}
			}
			return 0;
		}


		public int run( string[] args ) {
			if( args.Length == 0 ) {
				Debug.Log( "ripcmd make-cue [rip file] (PERFORMER=%name%)" );
				Debug.Log( "ripcmd file-adjust [rip file]" );
				return -1;
			}
			string a;
			if( ( a = args[ 0 ] ) != null ) {
				if( a == "make-cue" || a == "0" ) {
					return this.run2( args );
				}
				if( a == "file-adjust" || a == "1" ) {
					return this.run3( args );
				}
			}
			return -1;
		}
	}


	class Program {
		static int Main( string[] args ) {
			App app = new App();
			return app.run( args );
		}
	}
}
