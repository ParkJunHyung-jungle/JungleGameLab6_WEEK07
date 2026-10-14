// 1. 실제 플레이테스트 CSV 데이터 (PlaytestLogs 로드본)
const sampleCsvText = `session_id,build_version,sequence,key,event,elapsed_seconds,held_seconds,end_reason
ae2625e6658d4633b50662403c234290,0.1.0,1,A,Down,6.304664,,
ae2625e6658d4633b50662403c234290,0.1.0,2,S,Down,6.346135,,
ae2625e6658d4633b50662403c234290,0.1.0,3,D,Down,6.412341,,
ae2625e6658d4633b50662403c234290,0.1.0,4,A,Up,6.459154,0.154490,Released
ae2625e6658d4633b50662403c234290,0.1.0,5,F,Down,6.502268,,
ae2625e6658d4633b50662403c234290,0.1.0,6,S,Up,6.523452,0.177317,Released
ae2625e6658d4633b50662403c234290,0.1.0,7,D,Up,6.580107,0.167767,Released
ae2625e6658d4633b50662403c234290,0.1.0,8,F,Up,6.705107,0.202838,Released
ae2625e6658d4633b50662403c234290,0.1.0,9,A,Down,7.011294,,
ae2625e6658d4633b50662403c234290,0.1.0,10,S,Down,7.096569,,
ae2625e6658d4633b50662403c234290,0.1.0,11,D,Down,7.175369,,
ae2625e6658d4633b50662403c234290,0.1.0,12,A,Up,7.208207,0.196913,Released
ae2625e6658d4633b50662403c234290,0.1.0,13,F,Down,7.251110,,
ae2625e6658d4633b50662403c234290,0.1.0,14,S,Up,7.296842,0.200273,Released
ae2625e6658d4633b50662403c234290,0.1.0,15,D,Up,7.342347,0.166977,Released
ae2625e6658d4633b50662403c234290,0.1.0,16,F,Up,7.410076,0.158966,Released
ae2625e6658d4633b50662403c234290,0.1.0,17,Q,Down,7.524343,,
ae2625e6658d4633b50662403c234290,0.1.0,18,W,Down,7.622051,,
ae2625e6658d4633b50662403c234290,0.1.0,19,E,Down,7.700787,,
ae2625e6658d4633b50662403c234290,0.1.0,20,Q,Up,7.739509,0.215167,Released
ae2625e6658d4633b50662403c234290,0.1.0,21,R,Down,7.787667,,
ae2625e6658d4633b50662403c234290,0.1.0,22,W,Up,7.819929,0.197878,Released
ae2625e6658d4633b50662403c234290,0.1.0,23,E,Up,7.868686,0.167898,Released
ae2625e6658d4633b50662403c234290,0.1.0,24,R,Up,7.956609,0.168943,Released
ae2625e6658d4633b50662403c234290,0.1.0,25,T,Down,8.103129,,
ae2625e6658d4633b50662403c234290,0.1.0,26,T,Up,8.248225,0.145096,Released
ae2625e6658d4633b50662403c234290,0.1.0,27,T,Down,8.361032,,
ae2625e6658d4633b50662403c234290,0.1.0,28,Y,Down,8.361032,,
ae2625e6658d4633b50662403c234290,0.1.0,29,T,Up,8.439844,0.078812,Released
ae2625e6658d4633b50662403c234290,0.1.0,30,Y,Up,8.439844,0.078812,Released
ae2625e6658d4633b50662403c234290,0.1.0,31,Y,Down,8.553985,,
ae2625e6658d4633b50662403c234290,0.1.0,32,U,Down,8.557736,,
ae2625e6658d4633b50662403c234290,0.1.0,33,U,Up,8.627863,0.070126,Released
ae2625e6658d4633b50662403c234290,0.1.0,34,Y,Up,8.638818,0.084832,Released
ae2625e6658d4633b50662403c234290,0.1.0,35,I,Down,8.815271,,
ae2625e6658d4633b50662403c234290,0.1.0,36,J,Down,8.831565,,
ae2625e6658d4633b50662403c234290,0.1.0,37,J,Up,8.870356,0.038791,Released
ae2625e6658d4633b50662403c234290,0.1.0,38,I,Up,8.902941,0.087670,Released
ae2625e6658d4633b50662403c234290,0.1.0,39,K,Down,9.033901,,
ae2625e6658d4633b50662403c234290,0.1.0,40,U,Down,9.047296,,
ae2625e6658d4633b50662403c234290,0.1.0,41,U,Up,9.162996,0.115700,Released
ae2625e6658d4633b50662403c234290,0.1.0,42,K,Up,9.173954,0.140052,Released
ae2625e6658d4633b50662403c234290,0.1.0,43,J,Down,9.305944,,
ae2625e6658d4633b50662403c234290,0.1.0,44,G,Down,9.316864,,
ae2625e6658d4633b50662403c234290,0.1.0,45,H,Down,9.320349,,
ae2625e6658d4633b50662403c234290,0.1.0,46,H,Up,9.396776,0.076427,Released
ae2625e6658d4633b50662403c234290,0.1.0,47,G,Up,9.402837,0.085972,Released
ae2625e6658d4633b50662403c234290,0.1.0,48,J,Up,9.402837,0.096892,Released
ae2625e6658d4633b50662403c234290,0.1.0,49,H,Down,9.516903,,
ae2625e6658d4633b50662403c234290,0.1.0,50,G,Down,9.525534,,
ae2625e6658d4633b50662403c234290,0.1.0,51,J,Down,9.525534,,
ae2625e6658d4633b50662403c234290,0.1.0,52,G,Up,9.647191,0.121657,Released
ae2625e6658d4633b50662403c234290,0.1.0,53,J,Up,9.650650,0.125116,Released
ae2625e6658d4633b50662403c234290,0.1.0,54,H,Up,9.666269,0.149366,Released
ae2625e6658d4633b50662403c234290,0.1.0,55,Q,Down,13.702973,,
ae2625e6658d4633b50662403c234290,0.1.0,56,W,Down,13.780648,,
ae2625e6658d4633b50662403c234290,0.1.0,57,E,Down,13.826104,,
ae2625e6658d4633b50662403c234290,0.1.0,58,R,Down,13.901267,,
ae2625e6658d4633b50662403c234290,0.1.0,59,Q,Up,13.919896,0.216923,Released
ae2625e6658d4633b50662403c234290,0.1.0,60,W,Up,13.973874,0.193226,Released
ae2625e6658d4633b50662403c234290,0.1.0,61,E,Up,14.007040,0.180935,Released
ae2625e6658d4633b50662403c234290,0.1.0,62,R,Up,14.100437,0.199171,Released
ae2625e6658d4633b50662403c234290,0.1.0,63,T,Down,14.816358,,
ae2625e6658d4633b50662403c234290,0.1.0,64,Y,Down,14.847028,,
ae2625e6658d4633b50662403c234290,0.1.0,65,U,Down,14.972999,,
ae2625e6658d4633b50662403c234290,0.1.0,66,T,Up,14.993970,0.177612,Released
ae2625e6658d4633b50662403c234290,0.1.0,67,Y,Up,15.096822,0.249794,Released
ae2625e6658d4633b50662403c234290,0.1.0,68,I,Down,15.127357,,
ae2625e6658d4633b50662403c234290,0.1.0,69,U,Up,15.181166,0.208167,Released
ae2625e6658d4633b50662403c234290,0.1.0,70,I,Up,15.300128,0.172771,Released
ae2625e6658d4633b50662403c234290,0.1.0,71,Y,Down,15.610637,,
ae2625e6658d4633b50662403c234290,0.1.0,72,T,Down,15.730503,,
ae2625e6658d4633b50662403c234290,0.1.0,73,I,Down,15.860430,,
ae2625e6658d4633b50662403c234290,0.1.0,74,U,Down,15.867360,,
ae2625e6658d4633b50662403c234290,0.1.0,75,Y,Up,15.885839,0.275202,Released
ae2625e6658d4633b50662403c234290,0.1.0,76,T,Up,15.934596,0.204092,Released
ae2625e6658d4633b50662403c234290,0.1.0,77,Y,Down,15.977472,,
ae2625e6658d4633b50662403c234290,0.1.0,78,I,Up,16.038675,0.178245,Released
ae2625e6658d4633b50662403c234290,0.1.0,79,U,Up,16.093835,0.226475,Released
ae2625e6658d4633b50662403c234290,0.1.0,80,T,Down,16.116656,,
ae2625e6658d4633b50662403c234290,0.1.0,81,Y,Up,16.131053,0.153580,Released
ae2625e6658d4633b50662403c234290,0.1.0,82,T,Up,16.171305,0.054649,Released
ae2625e6658d4633b50662403c234290,0.1.0,83,O,Down,16.529723,,
ae2625e6658d4633b50662403c234290,0.1.0,84,P,Down,16.529723,,
ae2625e6658d4633b50662403c234290,0.1.0,85,P,Up,16.726000,0.196276,Released
ae2625e6658d4633b50662403c234290,0.1.0,86,O,Up,16.742718,0.212994,Released
ae2625e6658d4633b50662403c234290,0.1.0,87,A,Down,17.045535,,
ae2625e6658d4633b50662403c234290,0.1.0,88,S,Down,17.057924,,
ae2625e6658d4633b50662403c234290,0.1.0,89,D,Down,17.075374,,
ae2625e6658d4633b50662403c234290,0.1.0,90,F,Down,17.216991,,
ae2625e6658d4633b50662403c234290,0.1.0,91,A,Up,17.329001,0.283465,Released
ae2625e6658d4633b50662403c234290,0.1.0,92,S,Up,17.382776,0.324852,Released
ae2625e6658d4633b50662403c234290,0.1.0,93,D,Up,17.426979,0.351605,Released
ae2625e6658d4633b50662403c234290,0.1.0,94,F,Up,17.460264,0.243274,Released
ae2625e6658d4633b50662403c234290,0.1.0,95,G,Down,17.509501,,
ae2625e6658d4633b50662403c234290,0.1.0,96,H,Down,17.569537,,
ae2625e6658d4633b50662403c234290,0.1.0,97,J,Down,17.703305,,
ae2625e6658d4633b50662403c234290,0.1.0,98,G,Up,17.807143,0.297642,Released
ae2625e6658d4633b50662403c234290,0.1.0,99,H,Up,17.853557,0.284020,Released
ae2625e6658d4633b50662403c234290,0.1.0,100,J,Up,17.867193,0.163888,Released
ae2625e6658d4633b50662403c234290,0.1.0,101,J,Down,17.872299,,
ae2625e6658d4633b50662403c234290,0.1.0,102,J,Up,17.872299,0.000000,Released
ae2625e6658d4633b50662403c234290,0.1.0,103,L,Down,18.124297,,
ae2625e6658d4633b50662403c234290,0.1.0,104,K,Down,18.133820,,
ae2625e6658d4633b50662403c234290,0.1.0,105,J,Down,18.140077,,
ae2625e6658d4633b50662403c234290,0.1.0,106,J,Up,18.238991,0.098915,Released
ae2625e6658d4633b50662403c234290,0.1.0,107,K,Up,18.297417,0.163597,Released
ae2625e6658d4633b50662403c234290,0.1.0,108,L,Up,18.327863,0.203566,Released
ae2625e6658d4633b50662403c234290,0.1.0,109,Z,Down,18.707731,,
ae2625e6658d4633b50662403c234290,0.1.0,110,X,Down,18.736602,,
ae2625e6658d4633b50662403c234290,0.1.0,111,C,Down,18.799346,,
ae2625e6658d4633b50662403c234290,0.1.0,112,V,Down,18.899322,,
ae2625e6658d4633b50662403c234290,0.1.0,113,Z,Up,18.925392,0.217661,Released
ae2625e6658d4633b50662403c234290,0.1.0,114,X,Up,18.997428,0.260826,Released
ae2625e6658d4633b50662403c234290,0.1.0,115,C,Up,19.044426,0.245080,Released
ae2625e6658d4633b50662403c234290,0.1.0,116,V,Up,19.118650,0.219328,Released
ae2625e6658d4633b50662403c234290,0.1.0,117,C,Down,19.520007,,
ae2625e6658d4633b50662403c234290,0.1.0,118,C,Up,19.644267,0.124261,Released
ae2625e6658d4633b50662403c234290,0.1.0,119,C,Down,19.751955,,
ae2625e6658d4633b50662403c234290,0.1.0,120,C,Up,19.818970,0.067016,Released
ae2625e6658d4633b50662403c234290,0.1.0,121,X,Down,20.108295,,
ae2625e6658d4633b50662403c234290,0.1.0,122,X,Up,20.207891,0.099596,Released
ae2625e6658d4633b50662403c234290,0.1.0,123,X,Down,20.308987,,
ae2625e6658d4633b50662403c234290,0.1.0,124,X,Up,20.433530,0.124544,Released
ae2625e6658d4633b50662403c234290,0.1.0,125,X,Down,20.598041,,
ae2625e6658d4633b50662403c234290,0.1.0,126,X,Up,20.691247,0.093207,Released
ae2625e6658d4633b50662403c234290,0.1.0,127,X,Down,20.771870,,
ae2625e6658d4633b50662403c234290,0.1.0,128,X,Up,20.861960,0.090089,Released
ae2625e6658d4633b50662403c234290,0.1.0,129,X,Down,20.947770,,
ae2625e6658d4633b50662403c234290,0.1.0,130,X,Up,21.061795,0.114024,Released
ae2625e6658d4633b50662403c234290,0.1.0,131,Backspace,Down,21.601512,,
ae2625e6658d4633b50662403c234290,0.1.0,132,Backspace,Up,21.678975,0.077463,Released
ae2625e6658d4633b50662403c234290,0.1.0,133,Backspace,Down,28.271613,,
ae2625e6658d4633b50662403c234290,0.1.0,134,Backspace,Up,28.360970,0.089357,Released
ae2625e6658d4633b50662403c234290,0.1.0,135,Q,Down,31.382796,,
ae2625e6658d4633b50662403c234290,0.1.0,136,W,Down,31.461495,,
ae2625e6658d4633b50662403c234290,0.1.0,137,E,Down,31.556248,,
ae2625e6658d4633b50662403c234290,0.1.0,138,Q,Up,31.605082,0.222286,Released
ae2625e6658d4633b50662403c234290,0.1.0,139,R,Down,31.634388,,
ae2625e6658d4633b50662403c234290,0.1.0,140,W,Up,31.682815,0.221320,Released
ae2625e6658d4633b50662403c234290,0.1.0,141,E,Up,31.731844,0.175595,Released
ae2625e6658d4633b50662403c234290,0.1.0,142,R,Up,31.794349,0.159961,Released
ae2625e6658d4633b50662403c234290,0.1.0,143,Tab,Down,32.554817,,
ae2625e6658d4633b50662403c234290,0.1.0,144,Tab,Up,32.626379,0.071562,Released
ae2625e6658d4633b50662403c234290,0.1.0,145,Tab,Down,53.031535,,
ae2625e6658d4633b50662403c234290,0.1.0,146,Tab,Up,53.152591,0.121056,Released
ae2625e6658d4633b50662403c234290,0.1.0,147,A,Down,55.581236,,
ae2625e6658d4633b50662403c234290,0.1.0,148,L,Down,55.709885,,
ae2625e6658d4633b50662403c234290,0.1.0,149,S,Down,55.713712,,
ae2625e6658d4633b50662403c234290,0.1.0,150,D,Down,55.750403,,
ae2625e6658d4633b50662403c234290,0.1.0,151,K,Down,55.773731,,
ae2625e6658d4633b50662403c234290,0.1.0,152,A,Up,55.788192,0.206956,Released
ae2625e6658d4633b50662403c234290,0.1.0,153,L,Up,55.857312,0.147427,Released
ae2625e6658d4633b50662403c234290,0.1.0,154,F,Down,55.887782,,
ae2625e6658d4633b50662403c234290,0.1.0,155,S,Up,55.895141,0.181429,Released
ae2625e6658d4633b50662403c234290,0.1.0,156,J,Down,55.904048,,
ae2625e6658d4633b50662403c234290,0.1.0,157,K,Up,55.951502,0.177771,Released
ae2625e6658d4633b50662403c234290,0.1.0,158,D,Up,55.958022,0.207619,Released
ae2625e6658d4633b50662403c234290,0.1.0,159,J,Up,56.030836,0.126788,Released
ae2625e6658d4633b50662403c234290,0.1.0,160,L,Down,56.048341,,
ae2625e6658d4633b50662403c234290,0.1.0,161,F,Up,56.051633,0.163851,Released
ae2625e6658d4633b50662403c234290,0.1.0,162,Semicolon,Down,56.063401,,
ae2625e6658d4633b50662403c234290,0.1.0,163,K,Down,56.132822,,
ae2625e6658d4633b50662403c234290,0.1.0,164,Semicolon,Up,56.161401,0.098001,Released
ae2625e6658d4633b50662403c234290,0.1.0,165,J,Down,56.203561,,
ae2625e6658d4633b50662403c234290,0.1.0,166,L,Up,56.215300,0.166959,Released
ae2625e6658d4633b50662403c234290,0.1.0,167,K,Up,56.249182,0.116361,Released
ae2625e6658d4633b50662403c234290,0.1.0,168,J,Up,56.337886,0.134324,Released
ae2625e6658d4633b50662403c234290,0.1.0,169,O,Down,56.403095,,
ae2625e6658d4633b50662403c234290,0.1.0,170,Semicolon,Down,56.409240,,
ae2625e6658d4633b50662403c234290,0.1.0,171,P,Down,56.452928,,
ae2625e6658d4633b50662403c234290,0.1.0,172,O,Up,56.459030,0.055936,Released
ae2625e6658d4633b50662403c234290,0.1.0,173,Semicolon,Up,56.473872,0.064632,Released
ae2625e6658d4633b50662403c234290,0.1.0,174,P,Up,56.480042,0.027114,Released
ae2625e6658d4633b50662403c234290,0.1.0,175,I,Down,56.483929,,
ae2625e6658d4633b50662403c234290,0.1.0,176,I,Up,56.558805,0.074876,Released
ae2625e6658d4633b50662403c234290,0.1.0,177,S,Down,56.591615,,
ae2625e6658d4633b50662403c234290,0.1.0,178,S,Up,56.681404,0.089789,Released
ae2625e6658d4633b50662403c234290,0.1.0,179,S,Down,56.778959,,
ae2625e6658d4633b50662403c234290,0.1.0,180,S,Up,56.865155,0.086196,Released
ae2625e6658d4633b50662403c234290,0.1.0,181,D,Down,56.957650,,
ae2625e6658d4633b50662403c234290,0.1.0,182,D,Up,57.054314,0.096664,Released
ae2625e6658d4633b50662403c234290,0.1.0,183,D,Down,57.134859,,
ae2625e6658d4633b50662403c234290,0.1.0,184,D,Up,57.207151,0.072292,Released
ae2625e6658d4633b50662403c234290,0.1.0,185,F,Down,57.305514,,
ae2625e6658d4633b50662403c234290,0.1.0,186,F,Up,57.436988,0.131474,Released
ae2625e6658d4633b50662403c234290,0.1.0,187,S,Down,57.530795,,
ae2625e6658d4633b50662403c234290,0.1.0,188,S,Up,57.617973,0.087178,Released
ae2625e6658d4633b50662403c234290,0.1.0,189,A,Down,57.718701,,
ae2625e6658d4633b50662403c234290,0.1.0,190,S,Down,57.853448,,
ae2625e6658d4633b50662403c234290,0.1.0,191,D,Down,57.868307,,
ae2625e6658d4633b50662403c234290,0.1.0,192,A,Up,57.913526,0.194825,Released
ae2625e6658d4633b50662403c234290,0.1.0,193,F,Down,57.955651,,
ae2625e6658d4633b50662403c234290,0.1.0,194,S,Up,57.987180,0.133733,Released
ae2625e6658d4633b50662403c234290,0.1.0,195,D,Up,58.038709,0.170402,Released
ae2625e6658d4633b50662403c234290,0.1.0,196,F,Up,58.125074,0.169422,Released
ae2625e6658d4633b50662403c234290,0.1.0,197,A,Down,58.298684,,
ae2625e6658d4633b50662403c234290,0.1.0,198,A,Up,58.457996,0.159312,Released
ae2625e6658d4633b50662403c234290,0.1.0,199,A,Down,58.759874,,
ae2625e6658d4633b50662403c234290,0.1.0,200,A,Up,58.855789,0.095915,Released
ae2625e6658d4633b50662403c234290,0.1.0,201,A,Down,58.937726,,
ae2625e6658d4633b50662403c234290,0.1.0,202,A,Up,59.031899,0.094174,Released
ae2625e6658d4633b50662403c234290,0.1.0,203,A,Down,59.136417,,
ae2625e6658d4633b50662403c234290,0.1.0,204,A,Up,59.244333,0.107916,Released
ae2625e6658d4633b50662403c234290,0.1.0,205,Backspace,Down,59.331493,,
ae2625e6658d4633b50662403c234290,0.1.0,206,Backspace,Up,59.431324,0.099832,Released
ae2625e6658d4633b50662403c234290,0.1.0,207,LeftAlt,Down,60.446678,,
ae2625e6658d4633b50662403c234290,0.1.0,208,LeftAlt,Up,60.772501,0.325823,Released
ae2625e6658d4633b50662403c234290,0.1.0,209,A,Down,62.997004,,
ae2625e6658d4633b50662403c234290,0.1.0,210,W,Down,63.083091,,
ae2625e6658d4633b50662403c234290,0.1.0,211,A,Up,63.263013,0.266009,Released
ae2625e6658d4633b50662403c234290,0.1.0,212,W,Up,63.380199,0.297108,Released
ae2625e6658d4633b50662403c234290,0.1.0,213,W,Down,63.967386,,
ae2625e6658d4633b50662403c234290,0.1.0,214,W,Up,64.211021,0.243635,Released
ae2625e6658d4633b50662403c234290,0.1.0,215,Q,Down,66.434445,,
ae2625e6658d4633b50662403c234290,0.1.0,216,W,Down,66.500676,,
ae2625e6658d4633b50662403c234290,0.1.0,217,O,Down,66.543137,,
ae2625e6658d4633b50662403c234290,0.1.0,218,E,Down,66.567866,,
ae2625e6658d4633b50662403c234290,0.1.0,219,R,Down,66.647283,,
ae2625e6658d4633b50662403c234290,0.1.0,220,Q,Up,66.654118,0.219672,Released
ae2625e6658d4633b50662403c234290,0.1.0,221,O,Up,66.657956,0.114819,Released
ae2625e6658d4633b50662403c234290,0.1.0,222,W,Up,66.700085,0.199409,Released
ae2625e6658d4633b50662403c234290,0.1.0,223,U,Down,66.716318,,
ae2625e6658d4633b50662403c234290,0.1.0,224,E,Up,66.741284,0.173418,Released
ae2625e6658d4633b50662403c234290,0.1.0,225,R,Up,66.801473,0.154190,Released
ae2625e6658d4633b50662403c234290,0.1.0,226,O,Down,66.816833,,
ae2625e6658d4633b50662403c234290,0.1.0,227,P,Down,66.832953,,
ae2625e6658d4633b50662403c234290,0.1.0,228,U,Up,66.842445,0.126126,Released
ae2625e6658d4633b50662403c234290,0.1.0,229,I,Down,66.926548,,
ae2625e6658d4633b50662403c234290,0.1.0,230,P,Up,66.942454,0.109501,Released
ae2625e6658d4633b50662403c234290,0.1.0,231,U,Down,66.982758,,
ae2625e6658d4633b50662403c234290,0.1.0,232,T,Down,66.989574,,
ae2625e6658d4633b50662403c234290,0.1.0,233,O,Up,66.999726,0.182893,Released
ae2625e6658d4633b50662403c234290,0.1.0,234,I,Up,67.051555,0.125006,Released
ae2625e6658d4633b50662403c234290,0.1.0,235,U,Up,67.117287,0.134529,Released
ae2625e6658d4633b50662403c234290,0.1.0,236,T,Up,67.133336,0.143763,Released
ae2625e6658d4633b50662403c234290,0.1.0,237,Q,Down,67.371769,,
ae2625e6658d4633b50662403c234290,0.1.0,238,W,Down,67.431797,,
ae2625e6658d4633b50662403c234290,0.1.0,239,E,Down,67.511269,,
ae2625e6658d4633b50662403c234290,0.1.0,240,R,Down,67.605608,,
ae2625e6658d4633b50662403c234290,0.1.0,241,Q,Up,67.641605,0.269836,Released
ae2625e6658d4633b50662403c234290,0.1.0,242,W,Up,67.697654,0.265856,Released
ae2625e6658d4633b50662403c234290,0.1.0,243,E,Up,67.736421,0.225152,Released
ae2625e6658d4633b50662403c234290,0.1.0,244,R,Up,67.830286,0.224678,Released
ae2625e6658d4633b50662403c234290,0.1.0,245,W,Down,67.986367,,
ae2625e6658d4633b50662403c234290,0.1.0,246,W,Up,68.103720,0.117352,Released
ae2625e6658d4633b50662403c234290,0.1.0,247,E,Down,68.393632,,
ae2625e6658d4633b50662403c234290,0.1.0,248,E,Up,68.506108,0.112476,Released
ae2625e6658d4633b50662403c234290,0.1.0,249,E,Down,68.641885,,
ae2625e6658d4633b50662403c234290,0.1.0,250,E,Up,68.752406,0.110521,Released
ae2625e6658d4633b50662403c234290,0.1.0,251,R,Down,68.824817,,
ae2625e6658d4633b50662403c234290,0.1.0,252,R,Up,68.932705,0.107889,Released
ae2625e6658d4633b50662403c234290,0.1.0,253,R,Down,69.027915,,
ae2625e6658d4633b50662403c234290,0.1.0,254,R,Up,69.117902,0.089988,Released
ae2625e6658d4633b50662403c234290,0.1.0,255,W,Down,69.237376,,
ae2625e6658d4633b50662403c234290,0.1.0,256,W,Up,69.327195,0.089819,Released
ae2625e6658d4633b50662403c234290,0.1.0,257,W,Down,69.433227,,
ae2625e6658d4633b50662403c234290,0.1.0,258,W,Up,69.522212,0.088985,Released
ae2625e6658d4633b50662403c234290,0.1.0,259,Q,Down,69.635711,,
ae2625e6658d4633b50662403c234290,0.1.0,260,Q,Up,69.741357,0.105645,Released
ae2625e6658d4633b50662403c234290,0.1.0,261,Q,Down,69.850200,,
ae2625e6658d4633b50662403c234290,0.1.0,262,Q,Up,69.926216,0.076017,Released
ae2625e6658d4633b50662403c234290,0.1.0,263,Q,Down,70.038100,,
ae2625e6658d4633b50662403c234290,0.1.0,264,Q,Up,70.119901,0.081801,Released
ae2625e6658d4633b50662403c234290,0.1.0,265,Q,Down,70.226721,,
ae2625e6658d4633b50662403c234290,0.1.0,266,Q,Up,70.324998,0.098277,Released
ae2625e6658d4633b50662403c234290,0.1.0,267,Backspace,Down,70.944486,,
ae2625e6658d4633b50662403c234290,0.1.0,268,Backspace,Up,71.021245,0.076759,Released
ae2625e6658d4633b50662403c234290,0.1.0,269,A,Down,74.278870,,
ae2625e6658d4633b50662403c234290,0.1.0,270,W,Down,74.663995,,
ae2625e6658d4633b50662403c234290,0.1.0,271,A,Up,74.781283,0.502413,Released
ae2625e6658d4633b50662403c234290,0.1.0,272,W,Up,74.797667,0.133673,Released
ae2625e6658d4633b50662403c234290,0.1.0,273,Q,Down,76.595568,,
ae2625e6658d4633b50662403c234290,0.1.0,274,W,Down,76.726962,,
ae2625e6658d4633b50662403c234290,0.1.0,275,E,Down,76.789466,,
ae2625e6658d4633b50662403c234290,0.1.0,276,Q,Up,76.839336,0.243767,Released
ae2625e6658d4633b50662403c234290,0.1.0,277,R,Down,76.895299,,
ae2625e6658d4633b50662403c234290,0.1.0,278,W,Up,76.917158,0.190196,Released
ae2625e6658d4633b50662403c234290,0.1.0,279,E,Up,76.969113,0.179647,Released
ae2625e6658d4633b50662403c234290,0.1.0,280,R,Up,77.062706,0.167408,Released
ae2625e6658d4633b50662403c234290,0.1.0,281,T,Down,77.220664,,
ae2625e6658d4633b50662403c234290,0.1.0,282,Y,Down,77.224488,,
ae2625e6658d4633b50662403c234290,0.1.0,283,Y,Up,77.318516,0.094028,Released
ae2625e6658d4633b50662403c234290,0.1.0,284,T,Up,77.345624,0.124961,Released
ae2625e6658d4633b50662403c234290,0.1.0,285,Y,Down,77.493591,,
ae2625e6658d4633b50662403c234290,0.1.0,286,U,Down,77.614935,,
ae2625e6658d4633b50662403c234290,0.1.0,287,Y,Up,77.621536,0.127945,Released
ae2625e6658d4633b50662403c234290,0.1.0,288,U,Up,77.799167,0.184232,Released
ae2625e6658d4633b50662403c234290,0.1.0,289,I,Down,77.853029,,
ae2625e6658d4633b50662403c234290,0.1.0,290,I,Up,78.016253,0.163224,Released
ae2625e6658d4633b50662403c234290,0.1.0,291,O,Down,78.099449,,
ae2625e6658d4633b50662403c234290,0.1.0,292,O,Up,78.254286,0.154836,Released
ae2625e6658d4633b50662403c234290,0.1.0,293,P,Down,78.399920,,
ae2625e6658d4633b50662403c234290,0.1.0,294,P,Up,78.521131,0.121210,Released
ae2625e6658d4633b50662403c234290,0.1.0,295,A,Down,79.156222,,
ae2625e6658d4633b50662403c234290,0.1.0,296,S,Down,79.256981,,
ae2625e6658d4633b50662403c234290,0.1.0,297,D,Down,79.341370,,
ae2625e6658d4633b50662403c234290,0.1.0,298,A,Up,79.368844,0.212623,Released
ae2625e6658d4633b50662403c234290,0.1.0,299,F,Down,79.434699,,
ae2625e6658d4633b50662403c234290,0.1.0,300,S,Up,79.464974,0.207993,Released
ae2625e6658d4633b50662403c234290,0.1.0,301,D,Up,79.509211,0.167841,Released
ae2625e6658d4633b50662403c234290,0.1.0,302,F,Up,79.617456,0.182757,Released
ae2625e6658d4633b50662403c234290,0.1.0,303,F,Down,79.873193,,
ae2625e6658d4633b50662403c234290,0.1.0,304,G,Down,79.889521,,
ae2625e6658d4633b50662403c234290,0.1.0,305,H,Down,79.925840,,
ae2625e6658d4633b50662403c234290,0.1.0,306,J,Down,79.986607,,
ae2625e6658d4633b50662403c234290,0.1.0,307,F,Up,80.056990,0.183797,Released
ae2625e6658d4633b50662403c234290,0.1.0,308,K,Down,80.078312,,
ae2625e6658d4633b50662403c234290,0.1.0,309,G,Up,80.134429,0.244908,Released
ae2625e6658d4633b50662403c234290,0.1.0,310,H,Up,80.201032,0.275192,Released
ae2625e6658d4633b50662403c234290,0.1.0,311,J,Up,80.218494,0.231887,Released
ae2625e6658d4633b50662403c234290,0.1.0,312,K,Up,80.260104,0.181792,Released
ae2625e6658d4633b50662403c234290,0.1.0,313,L,Down,80.512816,,
ae2625e6658d4633b50662403c234290,0.1.0,314,L,Up,80.641937,0.129121,Released
ae2625e6658d4633b50662403c234290,0.1.0,315,Z,Down,82.007817,,
ae2625e6658d4633b50662403c234290,0.1.0,316,X,Down,82.106810,,
ae2625e6658d4633b50662403c234290,0.1.0,317,C,Down,82.168368,,
ae2625e6658d4633b50662403c234290,0.1.0,318,Z,Up,82.224793,0.216977,Released
ae2625e6658d4633b50662403c234290,0.1.0,319,V,Down,82.260831,,
ae2625e6658d4633b50662403c234290,0.1.0,320,X,Up,82.305686,0.198877,Released
ae2625e6658d4633b50662403c234290,0.1.0,321,C,Up,82.346545,0.178177,Released
ae2625e6658d4633b50662403c234290,0.1.0,322,V,Up,82.447266,0.186435,Released
ae2625e6658d4633b50662403c234290,0.1.0,323,B,Down,82.625874,,
ae2625e6658d4633b50662403c234290,0.1.0,324,N,Down,82.625874,,
ae2625e6658d4633b50662403c234290,0.1.0,325,B,Up,82.723997,0.098123,Released
ae2625e6658d4633b50662403c234290,0.1.0,326,N,Up,82.723997,0.098123,Released
ae2625e6658d4633b50662403c234290,0.1.0,327,M,Down,83.084001,,
ae2625e6658d4633b50662403c234290,0.1.0,328,N,Down,83.094314,,
ae2625e6658d4633b50662403c234290,0.1.0,329,B,Down,83.104390,,
ae2625e6658d4633b50662403c234290,0.1.0,330,B,Up,83.114428,0.010038,Released
ae2625e6658d4633b50662403c234290,0.1.0,331,N,Up,83.233992,0.139678,Released
ae2625e6658d4633b50662403c234290,0.1.0,332,M,Up,83.240538,0.156538,Released
ae2625e6658d4633b50662403c234290,0.1.0,333,N,Down,83.442076,,
ae2625e6658d4633b50662403c234290,0.1.0,334,B,Down,83.446697,,
ae2625e6658d4633b50662403c234290,0.1.0,335,M,Down,83.446697,,
ae2625e6658d4633b50662403c234290,0.1.0,336,M,Up,83.523102,0.076405,Released
ae2625e6658d4633b50662403c234290,0.1.0,337,B,Up,83.533440,0.086743,Released
ae2625e6658d4633b50662403c234290,0.1.0,338,N,Up,83.533440,0.091363,Released
ae2625e6658d4633b50662403c234290,0.1.0,339,M,Down,83.673741,,
ae2625e6658d4633b50662403c234290,0.1.0,340,N,Down,83.673741,,
ae2625e6658d4633b50662403c234290,0.1.0,341,B,Down,83.681016,,
ae2625e6658d4633b50662403c234290,0.1.0,342,M,Up,83.792243,0.118502,Released
ae2625e6658d4633b50662403c234290,0.1.0,343,B,Up,83.799077,0.118061,Released
ae2625e6658d4633b50662403c234290,0.1.0,344,N,Up,83.799077,0.125337,Released
ae2625e6658d4633b50662403c234290,0.1.0,345,Enter,Down,85.949672,,
ae2625e6658d4633b50662403c234290,0.1.0,346,Enter,Up,86.007326,0.057654,Released
ae2625e6658d4633b50662403c234290,0.1.0,347,LeftAlt,Down,87.449153,,
ae2625e6658d4633b50662403c234290,0.1.0,348,LeftAlt,Up,87.579237,0.130084,Released
ae2625e6658d4633b50662403c234290,0.1.0,349,LeftAlt,Down,87.786986,,
ae2625e6658d4633b50662403c234290,0.1.0,350,LeftAlt,Up,87.865729,0.078743,FocusLost
ae2625e6658d4633b50662403c234290,0.1.0,351,A,Down,92.358879,,
ae2625e6658d4633b50662403c234290,0.1.0,352,W,Down,92.389950,,
ae2625e6658d4633b50662403c234290,0.1.0,353,W,Up,92.492933,0.102983,Released
ae2625e6658d4633b50662403c234290,0.1.0,354,A,Up,92.538328,0.179448,Released
ae2625e6658d4633b50662403c234290,0.1.0,355,A,Down,92.913682,,
ae2625e6658d4633b50662403c234290,0.1.0,356,W,Down,93.013782,,
ae2625e6658d4633b50662403c234290,0.1.0,357,W,Up,93.186422,0.172640,Released
ae2625e6658d4633b50662403c234290,0.1.0,358,A,Up,93.251100,0.337418,Released
ae2625e6658d4633b50662403c234290,0.1.0,359,Q,Down,96.744142,,
ae2625e6658d4633b50662403c234290,0.1.0,360,W,Down,96.822542,,
ae2625e6658d4633b50662403c234290,0.1.0,361,E,Down,96.875289,,
ae2625e6658d4633b50662403c234290,0.1.0,362,Q,Up,96.959174,0.215032,Released
ae2625e6658d4633b50662403c234290,0.1.0,363,R,Down,96.974871,,
ae2625e6658d4633b50662403c234290,0.1.0,364,W,Up,97.015130,0.192588,Released
ae2625e6658d4633b50662403c234290,0.1.0,365,E,Up,97.070101,0.194812,Released
ae2625e6658d4633b50662403c234290,0.1.0,366,R,Up,97.139051,0.164179,Released
ae2625e6658d4633b50662403c234290,0.1.0,367,W,Down,97.549804,,
ae2625e6658d4633b50662403c234290,0.1.0,368,W,Up,97.638149,0.088345,Released
ae2625e6658d4633b50662403c234290,0.1.0,369,E,Down,97.744275,,
ae2625e6658d4633b50662403c234290,0.1.0,370,E,Up,97.893272,0.148997,Released
ae2625e6658d4633b50662403c234290,0.1.0,371,R,Down,97.928375,,
ae2625e6658d4633b50662403c234290,0.1.0,372,R,Up,98.003190,0.074814,Released
ae2625e6658d4633b50662403c234290,0.1.0,373,E,Down,98.151975,,
ae2625e6658d4633b50662403c234290,0.1.0,374,E,Up,98.246914,0.094939,Released
ae2625e6658d4633b50662403c234290,0.1.0,375,E,Down,98.353804,,
ae2625e6658d4633b50662403c234290,0.1.0,376,E,Up,98.421230,0.067427,Released
ae2625e6658d4633b50662403c234290,0.1.0,377,W,Down,98.552989,,
ae2625e6658d4633b50662403c234290,0.1.0,378,W,Up,98.636361,0.083372,Released
ae2625e6658d4633b50662403c234290,0.1.0,379,W,Down,98.754438,,
ae2625e6658d4633b50662403c234290,0.1.0,380,W,Up,98.835977,0.081539,Released
ae2625e6658d4633b50662403c234290,0.1.0,381,W,Down,98.952077,,
ae2625e6658d4633b50662403c234290,0.1.0,382,W,Up,99.032303,0.080226,Released
ae2625e6658d4633b50662403c234290,0.1.0,383,W,Down,99.144007,,
ae2625e6658d4633b50662403c234290,0.1.0,384,W,Up,99.229789,0.085782,Released
ae2625e6658d4633b50662403c234290,0.1.0,385,W,Down,99.341430,,
ae2625e6658d4633b50662403c234290,0.1.0,386,W,Up,99.424835,0.083405,Released
ae2625e6658d4633b50662403c234290,0.1.0,387,W,Down,100.653310,,
ae2625e6658d4633b50662403c234290,0.1.0,388,W,Up,100.752102,0.098792,Released
ae2625e6658d4633b50662403c234290,0.1.0,389,W,Down,101.192077,,
ae2625e6658d4633b50662403c234290,0.1.0,390,W,Up,101.282322,0.090245,Released
ae2625e6658d4633b50662403c234290,0.1.0,391,W,Down,101.710926,,
ae2625e6658d4633b50662403c234290,0.1.0,392,W,Up,101.812822,0.101896,Released
ae2625e6658d4633b50662403c234290,0.1.0,393,Backspace,Down,103.703334,,
ae2625e6658d4633b50662403c234290,0.1.0,394,Backspace,Up,103.794928,0.091594,Released
ae2625e6658d4633b50662403c234290,0.1.0,395,LeftAlt,Down,104.737431,,
ae2625e6658d4633b50662403c234290,0.1.0,396,LeftAlt,Up,104.876556,0.139125,FocusLost`;

/*
// [주석 보존] 향후 플레이어 ID 및 키보드 타입별 분리 분석 시 재사용할 Mock Dataset
const mockDataset = {
  "Player 1": {
    code: "Player 1",
    name: "테스터 Alpha",
    keyboards: {
      "KeyboardType A": { keyCounts: { "Space": 88, "W": 64, "D": 51, "A": 46, "S": 39 } },
      "KeyboardType B": { keyCounts: { "Space": 165, "Q": 102, "E": 94, "R": 88 } },
      "KeyboardType C": { keyCounts: { "ArrowUp": 88, "ArrowDown": 80, "Space": 62 } }
    }
  }
};
*/

// 2. 텐키리스(TKL, 87키) 키보드 물리 레이아웃 정의
const mainLayout = [
  // F-열 (Row 0)
  [
    { code: "Escape", label: "ESC" },
    { spacer: "f-gap-large" },
    { code: "F1", label: "F1" },
    { code: "F2", label: "F2" },
    { code: "F3", label: "F3" },
    { code: "F4", label: "F4" },
    { spacer: "f-gap" },
    { code: "F5", label: "F5" },
    { code: "F6", label: "F6" },
    { code: "F7", label: "F7" },
    { code: "F8", label: "F8" },
    { spacer: "f-gap" },
    { code: "F9", label: "F9" },
    { code: "F10", label: "F10" },
    { code: "F11", label: "F11" },
    { code: "F12", label: "F12" },
  ],
  // 숫자열 (Row 1)
  [
    { code: "Backquote", label: "~" },
    { code: "Digit1", label: "1" },
    { code: "Digit2", label: "2" },
    { code: "Digit3", label: "3" },
    { code: "Digit4", label: "4" },
    { code: "Digit5", label: "5" },
    { code: "Digit6", label: "6" },
    { code: "Digit7", label: "7" },
    { code: "Digit8", label: "8" },
    { code: "Digit9", label: "9" },
    { code: "Digit0", label: "0" },
    { code: "Minus", label: "-" },
    { code: "Equal", label: "=" },
    { code: "Backspace", label: "Backspace", widthClass: "w-2" },
  ],
  // QWERTY열 (Row 2)
  [
    { code: "Tab", label: "Tab", widthClass: "w-1-5" },
    { code: "Q", label: "Q" },
    { code: "W", label: "W" },
    { code: "E", label: "E" },
    { code: "R", label: "R" },
    { code: "T", label: "T" },
    { code: "Y", label: "Y" },
    { code: "U", label: "U" },
    { code: "I", label: "I" },
    { code: "O", label: "O" },
    { code: "P", label: "P" },
    { code: "BracketLeft", label: "[" },
    { code: "BracketRight", label: "]" },
    { code: "Backslash", label: "\\", widthClass: "w-1-5" },
  ],
  // 홈열 (Row 3)
  [
    { code: "CapsLock", label: "Caps", widthClass: "w-1-75" },
    { code: "A", label: "A" },
    { code: "S", label: "S" },
    { code: "D", label: "D" },
    { code: "F", label: "F" },
    { code: "G", label: "G" },
    { code: "H", label: "H" },
    { code: "J", label: "J" },
    { code: "K", label: "K" },
    { code: "L", label: "L" },
    { code: "Semicolon", label: ";" },
    { code: "Quote", label: "'" },
    { code: "Enter", label: "Enter", widthClass: "w-2-25" },
  ],
  // 하단열 (Row 4)
  [
    { code: "ShiftLeft", label: "Shift", widthClass: "w-2-25" },
    { code: "Z", label: "Z" },
    { code: "X", label: "X" },
    { code: "C", label: "C" },
    { code: "V", label: "V" },
    { code: "B", label: "B" },
    { code: "N", label: "N" },
    { code: "M", label: "M" },
    { code: "Comma", label: "<" },
    { code: "Period", label: ">" },
    { code: "Slash", label: "?" },
    { code: "ShiftRight", label: "Shift", widthClass: "w-2-75" },
  ],
  // 컨트롤/스페이스바열 (Row 5)
  [
    { code: "ControlLeft", label: "Ctrl", widthClass: "w-1-25" },
    { code: "MetaLeft", label: "Win", widthClass: "w-1-25" },
    { code: "AltLeft", label: "Alt", widthClass: "w-1-25" },
    { code: "Space", label: "Space", widthClass: "w-6-25" },
    { code: "AltRight", label: "Alt", widthClass: "w-1-25" },
    { code: "MetaRight", label: "Win", widthClass: "w-1-25" },
    { code: "ContextMenu", label: "Menu", widthClass: "w-1-25" },
    { code: "ControlRight", label: "Ctrl", widthClass: "w-1-25" },
  ]
];

const navLayout = [
  // F-열 상단 기능
  [
    { code: "PrintScreen", label: "PrtSc" },
    { code: "ScrollLock", label: "ScrLk" },
    { code: "Pause", label: "Pause" },
  ],
  // 6키 클러스터 상단
  [
    { code: "Insert", label: "Ins" },
    { code: "Home", label: "Home" },
    { code: "PageUp", label: "PgUp" },
  ],
  // 6키 클러스터 하단
  [
    { code: "Delete", label: "Del" },
    { code: "End", label: "End" },
    { code: "PageDown", label: "PgDn" },
  ],
  // 빈 여백 (Row 3)
  null,
  // 방향키 상단 (Row 4)
  [
    { empty: true },
    { code: "ArrowUp", label: "▲" },
    { empty: true },
  ],
  // 방향키 하단 (Row 5)
  [
    { code: "ArrowLeft", label: "◀" },
    { code: "ArrowDown", label: "▼" },
    { code: "ArrowRight", label: "▶" },
  ]
];

// 3. Unity Input System Key 문자열 정규화
function normalizeUnityKey(rawKey) {
  if (!rawKey) return "";
  const key = rawKey.trim();

  if (key === "LeftShift") return "ShiftLeft";
  if (key === "RightShift") return "ShiftRight";
  if (key === "LeftCtrl" || key === "LeftControl") return "ControlLeft";
  if (key === "RightCtrl" || key === "RightControl") return "ControlRight";
  if (key === "LeftAlt") return "AltLeft";
  if (key === "RightAlt") return "AltRight";
  if (key === "LeftCommand" || key === "LeftWindows") return "MetaLeft";
  if (key === "RightCommand" || key === "RightWindows") return "MetaRight";

  if (key === "Enter" || key === "Return" || key === "NumpadEnter") return "Enter";
  if (key === "Backspace") return "Backspace";
  if (key === "Escape") return "Escape";
  if (key === "Space") return "Space";
  if (key === "Tab") return "Tab";
  if (key === "CapsLock") return "CapsLock";

  if (key.startsWith("Digit")) return key;
  if (/^[0-9]$/.test(key)) return "Digit" + key;

  if (key === "UpArrow" || key === "ArrowUp") return "ArrowUp";
  if (key === "DownArrow" || key === "ArrowDown") return "ArrowDown";
  if (key === "LeftArrow" || key === "ArrowLeft") return "ArrowLeft";
  if (key === "RightArrow" || key === "ArrowRight") return "ArrowRight";

  if (key === "Backquote" || key === "`") return "Backquote";
  if (key === "Minus" || key === "-") return "Minus";
  if (key === "Equals" || key === "Equal" || key === "=") return "Equal";
  if (key === "LeftBracket" || key === "[") return "BracketLeft";
  if (key === "RightBracket" || key === "]") return "BracketRight";
  if (key === "Backslash" || key === "\\") return "Backslash";
  if (key === "Semicolon" || key === ";") return "Semicolon";
  if (key === "Quote" || key === "'") return "Quote";
  if (key === "Comma" || key === ",") return "Comma";
  if (key === "Period" || key === ".") return "Period";
  if (key === "Slash" || key === "/") return "Slash";

  if (/^[a-zA-Z]$/.test(key)) return key.toUpperCase();

  return key;
}

// 4. 히트맵 색상 계산
function getHeatmapColor(count, maxCount) {
  if (!count || count === 0) {
    return {
      bg: "var(--key-base)",
      border: "var(--key-border)",
      color: "var(--key-text)",
      boxShadow: "0 3px 0 rgba(0,0,0,0.4)"
    };
  }

  const ratio = Math.min(count / maxCount, 1.0);

  let r, g, b;
  if (ratio < 0.25) {
    const t = ratio / 0.25;
    r = Math.round(2 + t * 14);
    g = Math.round(132 + t * 50);
    b = Math.round(199 + t * -30);
  } else if (ratio < 0.5) {
    const t = (ratio - 0.25) / 0.25;
    r = Math.round(16 + t * 229);
    g = Math.round(185 + t * -27);
    b = Math.round(129 + t * -118);
  } else if (ratio < 0.75) {
    const t = (ratio - 0.5) / 0.25;
    r = Math.round(245 + t * -6);
    g = Math.round(158 + t * -90);
    b = Math.round(11 + t * 57);
  } else {
    const t = (ratio - 0.75) / 0.25;
    r = Math.round(239 + t * 16);
    g = Math.round(68 + t * -40);
    b = Math.round(68 + t * -30);
  }

  const glowOpacity = Math.max(0.2, ratio * 0.75);
  const shadow = "0 3px 0 rgba(0,0,0,0.4), 0 0 " + Math.round(ratio * 16) + "px rgba(" + r + ", " + g + ", " + b + ", " + glowOpacity + ")";

  return {
    bg: "rgba(" + r + ", " + g + ", " + b + ", " + (0.45 + ratio * 0.5) + ")",
    border: "rgb(" + r + ", " + g + ", " + b + ")",
    color: "#ffffff",
    boxShadow: shadow
  };
}

// 5. 키보드 히트맵 렌더링
function renderKeyboard(keyCounts, isAverage = false, rawTotalCounts = {}, sessionCount = 1) {
  const counts = Object.values(keyCounts);
  const maxCount = Math.max(...counts, 1);

  const legendText = document.getElementById("maxLegendText");
  if (legendText) {
    legendText.innerText = isAverage ? "최다 평균 (" + maxCount.toFixed(1) + "회)" : "최다 (" + maxCount.toLocaleString() + "회)";
  }

  const mainSection = document.getElementById("mainSection");
  const navSection = document.getElementById("navSection");
  if (!mainSection || !navSection) return;

  mainSection.innerHTML = "";
  navSection.innerHTML = "";

  const formatKeyCount = (count) => {
    if (!count || count === 0) return "";
    return isAverage ? (count % 1 === 0 ? count : count.toFixed(1)) : count.toLocaleString();
  };

  const getKeyTitle = (item, count) => {
    const displayCount = formatKeyCount(count);
    if (sessionCount > 1 && selectedSessionIndex === -1) {
      if (isAverage) {
        const total = rawTotalCounts[item.code] || 0;
        return `${item.label} (${item.code}): 세션 평균 ${displayCount}회 (총 ${total.toLocaleString()}회 / ${sessionCount}개 세션)`;
      } else {
        const avg = (count / sessionCount).toFixed(1);
        return `${item.label} (${item.code}): 총 ${displayCount}회 (세션 평균 ${avg}회 / ${sessionCount}개 세션)`;
      }
    }
    return `${item.label} (${item.code}): ${displayCount}회`;
  };

  mainLayout.forEach((row, rowIndex) => {
    const rowEl = document.createElement("div");
    rowEl.className = "kb-row" + (rowIndex === 0 ? " f-row" : "");

    row.forEach(item => {
      if (item.spacer) {
        const spacer = document.createElement("div");
        spacer.className = item.spacer;
        rowEl.appendChild(spacer);
        return;
      }

      const count = keyCounts[item.code] || 0;
      const style = getHeatmapColor(count, maxCount);

      const keyEl = document.createElement("div");
      keyEl.className = "key " + (item.widthClass || "") + (count > 0 ? " has-count" : "");
      keyEl.style.backgroundColor = style.bg;
      keyEl.style.borderColor = style.border;
      keyEl.style.color = style.color;
      keyEl.style.boxShadow = style.boxShadow;
      keyEl.title = getKeyTitle(item, count);

      const countStr = formatKeyCount(count);
      keyEl.innerHTML = "<span class=\"key-label\">" + item.label + "</span><span class=\"key-count\">" + countStr + "</span>";

      rowEl.appendChild(keyEl);
    });

    mainSection.appendChild(rowEl);
  });

  navLayout.forEach((row, rowIndex) => {
    if (!row) {
      const spacer = document.createElement("div");
      spacer.className = "nav-spacer";
      navSection.appendChild(spacer);
      return;
    }

    const rowEl = document.createElement("div");
    rowEl.className = "kb-row" + (rowIndex === 0 ? " f-row" : "");

    row.forEach(item => {
      if (item.empty) {
        const emptyEl = document.createElement("div");
        emptyEl.style.width = "var(--key-unit)";
        emptyEl.style.height = "var(--key-height)";
        rowEl.appendChild(emptyEl);
        return;
      }

      const count = keyCounts[item.code] || 0;
      const style = getHeatmapColor(count, maxCount);

      const keyEl = document.createElement("div");
      keyEl.className = "key" + (count > 0 ? " has-count" : "");
      keyEl.style.backgroundColor = style.bg;
      keyEl.style.borderColor = style.border;
      keyEl.style.color = style.color;
      keyEl.style.boxShadow = style.boxShadow;
      keyEl.title = getKeyTitle(item, count);

      const countStr = formatKeyCount(count);
      keyEl.innerHTML = "<span class=\"key-label\">" + item.label + "</span><span class=\"key-count\">" + countStr + "</span>";

      rowEl.appendChild(keyEl);
    });

    navSection.appendChild(rowEl);
  });
}

// 6. 상위 키 순위 차트 렌더링
function renderRanking(keyCounts, isAverage = false) {
  const rankingList = document.getElementById("rankingList");
  if (!rankingList) return;
  rankingList.innerHTML = "";

  const sortedKeys = Object.entries(keyCounts)
    .filter(([_, count]) => count > 0)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 10);

  const maxCount = sortedKeys[0] ? sortedKeys[0][1] : 1;
  const totalCount = Object.values(keyCounts).reduce((acc, cur) => acc + cur, 0);

  sortedKeys.forEach(([code, count], index) => {
    const percent = totalCount > 0 ? ((count / totalCount) * 100).toFixed(1) : "0.0";
    const barWidth = ((count / maxCount) * 100).toFixed(1);
    const rankClass = index < 3 ? "rank-" + (index + 1) : "";
    const countDisplay = isAverage ? (count % 1 === 0 ? count : count.toFixed(1)) : count.toLocaleString();
    const countLabel = isAverage ? `평균 ${countDisplay}회` : `${countDisplay}회`;

    const itemEl = document.createElement("div");
    itemEl.className = "ranking-item " + rankClass;
    itemEl.innerHTML = `
      <div class="ranking-header">
        <div class="ranking-key">
          <span class="rank-num">${index + 1}</span>
          <span>${code}</span>
        </div>
        <div class="ranking-stat">
          <strong>${countLabel}</strong> (${percent}%)
        </div>
      </div>
      <div class="bar-track">
        <div class="bar-fill" style="width: ${barWidth}%"></div>
      </div>
    `;

    rankingList.appendChild(itemEl);
  });
}

// ===================================================
// 7. 다중 세션 상태 관리 및 데이터 분석
// ===================================================
let loadedSessions = [];
let currentViewMode = "total"; // "total" | "average"
let selectedSessionIndex = -1; // -1: 전체, >= 0: loadedSessions 인덱스

// 단일 CSV 텍스트 파싱
function parseSingleCsv(csvContent, fileName = "Unknown") {
  if (!csvContent || csvContent.trim().length === 0) return null;

  const lines = csvContent.trim().split(/\r?\n/);
  if (lines.length < 2) return null;

  const header = lines[0].split(",").map(h => h.trim().toLowerCase());
  const colIndex = {
    sessionId: header.indexOf("session_id"),
    buildVersion: header.indexOf("build_version"),
    key: header.indexOf("key"),
    event: header.indexOf("event"),
    elapsedSeconds: header.indexOf("elapsed_seconds"),
    heldSeconds: header.indexOf("held_seconds"),
    endReason: header.indexOf("end_reason")
  };

  let sessionId = "-";
  let buildVersion = "-";
  let maxElapsed = 0;
  let totalDownCount = 0;
  const keyCounts = {};
  const downTimestamps = [];

  for (let i = 1; i < lines.length; i++) {
    const row = lines[i].split(",").map(v => v.trim());
    if (row.length < 5) continue;

    const rowSessionId = colIndex.sessionId !== -1 ? row[colIndex.sessionId] : "";
    const rowBuildVersion = colIndex.buildVersion !== -1 ? row[colIndex.buildVersion] : "";
    const rawKey = colIndex.key !== -1 ? row[colIndex.key] : "";
    const event = colIndex.event !== -1 ? row[colIndex.event].toLowerCase() : "";
    const elapsed = colIndex.elapsedSeconds !== -1 ? parseFloat(row[colIndex.elapsedSeconds]) : 0;

    if (rowSessionId && sessionId === "-") sessionId = rowSessionId;
    if (rowBuildVersion && buildVersion === "-") buildVersion = rowBuildVersion;
    if (!isNaN(elapsed) && elapsed > maxElapsed) maxElapsed = elapsed;

    if (event === "down") {
      const normalizedKey = normalizeUnityKey(rawKey);
      if (normalizedKey) {
        keyCounts[normalizedKey] = (keyCounts[normalizedKey] || 0) + 1;
        totalDownCount++;
        if (!isNaN(elapsed) && elapsed >= 0) {
          downTimestamps.push(elapsed);
        }
      }
    }
  }

  if (sessionId === "-") {
    sessionId = fileName.replace(/\.csv$/i, "");
  }

  return {
    fileName,
    sessionId,
    buildVersion,
    maxElapsed,
    totalDownCount,
    keyCounts,
    downTimestamps
  };
}

// 다중 세션 전체 종합 및 평균 계산
function getAggregatedData() {
  if (loadedSessions.length === 0) {
    return {
      sessionCount: 0,
      totalDownCount: 0,
      avgDownCount: 0,
      totalElapsed: 0,
      avgElapsed: 0,
      maxElapsed: 0,
      buildVersions: "-",
      rawTotalCounts: {},
      rawAvgCounts: {},
      keyCounts: {},
      downTimestamps: []
    };
  }

  const count = loadedSessions.length;
  let totalDown = 0;
  let totalElapsed = 0;
  let overallMaxElapsed = 0;
  const versions = new Set();
  const totalKeys = {};
  let allTimestamps = [];

  loadedSessions.forEach(session => {
    totalDown += session.totalDownCount;
    totalElapsed += session.maxElapsed;
    if (session.maxElapsed > overallMaxElapsed) {
      overallMaxElapsed = session.maxElapsed;
    }
    if (session.buildVersion && session.buildVersion !== "-") {
      versions.add(session.buildVersion);
    }
    for (const [key, val] of Object.entries(session.keyCounts)) {
      totalKeys[key] = (totalKeys[key] || 0) + val;
    }
    if (session.downTimestamps && session.downTimestamps.length > 0) {
      allTimestamps = allTimestamps.concat(session.downTimestamps);
    }
  });

  const avgKeys = {};
  for (const [key, val] of Object.entries(totalKeys)) {
    avgKeys[key] = Number((val / count).toFixed(2));
  }

  const avgDown = totalDown / count;
  const avgElapsed = totalElapsed / count;

  return {
    sessionCount: count,
    totalDownCount: totalDown,
    avgDownCount: avgDown,
    totalElapsed: totalElapsed,
    avgElapsed: avgElapsed,
    maxElapsed: overallMaxElapsed,
    buildVersions: versions.size > 0 ? Array.from(versions).join(", ") : "-",
    rawTotalCounts: totalKeys,
    rawAvgCounts: avgKeys,
    keyCounts: currentViewMode === "average" ? avgKeys : totalKeys,
    downTimestamps: allTimestamps
  };
}

// 8. 뷰 일괄 갱신 함수
function updateView() {
  const elSessionId = document.getElementById("sessionIdText");
  const elBuild = document.getElementById("buildVersionText");
  const elElapsed = document.getElementById("elapsedTimeText");
  const elDown = document.getElementById("downCountText");
  const elSummary = document.getElementById("summaryTotal");
  const elTypeTotal = document.getElementById("typeTotalBadge");
  const toolbar = document.getElementById("aggregateToolbar");
  const fileCountBadge = document.getElementById("fileCountBadge");
  const fileListTags = document.getElementById("fileListTags");
  const sessionSelect = document.getElementById("sessionSelect");

  // 글로벌 세션 네비게이션 카드 요소들
  const sessionGlobalCard = document.getElementById("sessionGlobalCard");
  const currentSessionAvatar = document.getElementById("currentSessionAvatar");
  const currentSessionTag = document.getElementById("currentSessionTag");
  const currentSessionBadge = document.getElementById("currentSessionBadge");
  const currentSessionName = document.getElementById("currentSessionName");
  const sessionTabs = document.getElementById("sessionTabs");

  if (loadedSessions.length === 0) {
    resetViewer();
    return;
  }

  // 단일 세션 로드 시
  if (loadedSessions.length === 1) {
    if (toolbar) toolbar.style.display = "none";
    if (fileCountBadge) fileCountBadge.style.display = "none";
    if (fileListTags) fileListTags.style.display = "none";
    if (sessionGlobalCard) sessionGlobalCard.style.display = "none";

    const session = loadedSessions[0];
    if (elSessionId) elSessionId.innerText = "Session: " + session.sessionId;
    if (elBuild) elBuild.innerText = "Build: " + session.buildVersion;
    if (elElapsed) elElapsed.innerText = "기록 시간: " + session.maxElapsed.toFixed(2) + "초";
    if (elDown) elDown.innerText = "총 누름(Down): " + session.totalDownCount.toLocaleString() + "회";
    if (elSummary) elSummary.innerText = "총 Down 입력: " + session.totalDownCount.toLocaleString() + "회";
    if (elTypeTotal) elTypeTotal.innerText = "총 Down 입력: " + session.totalDownCount.toLocaleString() + "회";

    renderKeyboard(session.keyCounts, false, session.keyCounts, 1);
    renderRanking(session.keyCounts, false);
    renderTimelineChart(session.downTimestamps, false, 1, session.maxElapsed);
    return;
  }

  // 다중 세션 로드 시 (2개 이상)
  if (toolbar) toolbar.style.display = "flex";
  if (fileCountBadge) {
    fileCountBadge.style.display = "inline-block";
    fileCountBadge.innerText = `${loadedSessions.length}개 파일 분석 중`;
  }
  if (fileListTags) {
    fileListTags.style.display = "flex";
    fileListTags.innerHTML = "";
    loadedSessions.forEach((s, idx) => {
      const tag = document.createElement("span");
      tag.className = "file-tag";
      tag.innerHTML = `📄 ${s.fileName} <button type="button" class="tag-remove-btn" title="이 파일 제외" data-index="${idx}">×</button>`;
      fileListTags.appendChild(tag);
    });
  }

  // 글로벌 세션 네비게이션 카드 표시 및 탭 렌더링
  if (sessionGlobalCard && sessionTabs) {
    sessionGlobalCard.style.display = "flex";
    sessionTabs.innerHTML = "";

    // 1) 전체 통합 탭
    const allTabBtn = document.createElement("button");
    allTabBtn.type = "button";
    allTabBtn.className = "player-tab-btn" + (selectedSessionIndex === -1 ? " active" : "");
    allTabBtn.setAttribute("data-index", "-1");
    allTabBtn.innerText = "📊 전체 통합";
    sessionTabs.appendChild(allTabBtn);

    // 2) 개별 세션 탭 목록
    loadedSessions.forEach((s, idx) => {
      const tabBtn = document.createElement("button");
      tabBtn.type = "button";
      tabBtn.className = "player-tab-btn" + (selectedSessionIndex === idx ? " active" : "");
      tabBtn.setAttribute("data-index", idx.toString());
      const shortName = s.fileName.length > 14 ? s.fileName.substring(0, 12) + ".." : s.fileName;
      tabBtn.innerText = `🎮 ${idx + 1}. ${shortName}`;
      tabBtn.title = s.fileName;
      sessionTabs.appendChild(tabBtn);
    });
  }

  // 세션 셀렉트 드롭다운 동기화
  if (sessionSelect) {
    sessionSelect.innerHTML = `<option value="ALL">전체 통합 분석 (${loadedSessions.length}개 세션)</option>`;
    loadedSessions.forEach((s, idx) => {
      const opt = document.createElement("option");
      opt.value = idx.toString();
      opt.innerText = `${idx + 1}. ${s.fileName} (${s.sessionId.substring(0, 8)}...)`;
      sessionSelect.appendChild(opt);
    });
    sessionSelect.value = selectedSessionIndex === -1 ? "ALL" : selectedSessionIndex.toString();
  }

  // 탭 활성화 상태
  const modeTotalBtn = document.getElementById("modeTotalBtn");
  const modeAverageBtn = document.getElementById("modeAverageBtn");
  if (modeTotalBtn && modeAverageBtn) {
    modeTotalBtn.classList.toggle("active", currentViewMode === "total");
    modeAverageBtn.classList.toggle("active", currentViewMode === "average");
  }

  // 1) 특정 개별 세션 조회 중인 경우
  if (selectedSessionIndex >= 0 && selectedSessionIndex < loadedSessions.length) {
    const session = loadedSessions[selectedSessionIndex];
    if (elSessionId) elSessionId.innerText = `[${selectedSessionIndex + 1}/${loadedSessions.length}] Session: ${session.sessionId}`;
    if (elBuild) elBuild.innerText = "Build: " + session.buildVersion;
    if (elElapsed) elElapsed.innerText = "기록 시간: " + session.maxElapsed.toFixed(2) + "초";
    if (elDown) elDown.innerText = "총 누름(Down): " + session.totalDownCount.toLocaleString() + "회";
    if (elSummary) elSummary.innerText = "개별 Down 입력: " + session.totalDownCount.toLocaleString() + "회";
    if (elTypeTotal) elTypeTotal.innerText = "개별 Down 입력: " + session.totalDownCount.toLocaleString() + "회";

    if (currentSessionAvatar) currentSessionAvatar.innerText = "🎮";
    if (currentSessionTag) currentSessionTag.innerText = `INDIVIDUAL SESSION #${selectedSessionIndex + 1}`;
    if (currentSessionBadge) currentSessionBadge.innerText = `Session ${selectedSessionIndex + 1}`;
    if (currentSessionName) currentSessionName.innerText = `${session.fileName} (기록 ${session.maxElapsed.toFixed(1)}초, ${session.totalDownCount.toLocaleString()}타)`;

    renderKeyboard(session.keyCounts, false, session.keyCounts, 1);
    renderRanking(session.keyCounts, false);
    renderTimelineChart(session.downTimestamps, false, 1, session.maxElapsed);
    return;
  }

  // 2) 전체 다중 세션 종합 또는 평균 조회 중인 경우
  const agg = getAggregatedData();
  const isAvg = currentViewMode === "average";

  if (currentSessionAvatar) currentSessionAvatar.innerText = "📊";
  if (currentSessionTag) currentSessionTag.innerText = `AGGREGATED SESSIONS (${agg.sessionCount} FILES)`;
  if (currentSessionBadge) currentSessionBadge.innerText = "ALL SESSIONS";
  if (currentSessionName) {
    currentSessionName.innerText = isAvg ? `전체 세션 통합 분석 (세션별 평균)` : `전체 세션 통합 분석 (종합 합계)`;
  }

  if (isAvg) {
    if (elSessionId) elSessionId.innerText = `총 ${agg.sessionCount}개 세션 평균 (Average 분석)`;
    if (elBuild) elBuild.innerText = `Build: ${agg.buildVersions}`;
    if (elElapsed) elElapsed.innerText = `세션 평균 기록 시간: ${agg.avgElapsed.toFixed(1)}초 (총 ${agg.totalElapsed.toFixed(1)}초)`;
    if (elDown) elDown.innerText = `세션 평균 누름(Down): ${agg.avgDownCount.toFixed(1)}회 (총 ${agg.totalDownCount.toLocaleString()}회)`;
    if (elSummary) elSummary.innerText = `세션 평균 Down: ${agg.avgDownCount.toFixed(1)}회`;
    if (elTypeTotal) elTypeTotal.innerText = `세션 평균 Down: ${agg.avgDownCount.toFixed(1)}회`;
  } else {
    if (elSessionId) elSessionId.innerText = `총 ${agg.sessionCount}개 세션 종합 (합계 Total 분석)`;
    if (elBuild) elBuild.innerText = `Build: ${agg.buildVersions}`;
    if (elElapsed) elElapsed.innerText = `총 기록 시간: ${agg.totalElapsed.toFixed(1)}초 (세션 평균 ${agg.avgElapsed.toFixed(1)}초)`;
    if (elDown) elDown.innerText = `총 누름(Down): ${agg.totalDownCount.toLocaleString()}회 (세션 평균 ${agg.avgDownCount.toFixed(1)}회)`;
    if (elSummary) elSummary.innerText = `총 Down 입력: ${agg.totalDownCount.toLocaleString()}회`;
    if (elTypeTotal) elTypeTotal.innerText = `총 Down 입력: ${agg.totalDownCount.toLocaleString()}회`;
  }

  renderKeyboard(agg.keyCounts, isAvg, agg.rawTotalCounts, agg.sessionCount);
  renderRanking(agg.keyCounts, isAvg);
  renderTimelineChart(agg.downTimestamps, isAvg, agg.sessionCount, agg.maxElapsed);
}

// 9. 시간대별 키 타격 횟수 및 타속 추이 차트 렌더링
let currentIntervalSetting = "5"; // "auto" | "1" | "2" | "5" | "10"
let chartHoverData = null;

function renderTimelineChart(timestamps, isAverage = false, sessionCount = 1, fixedMaxTime = null) {
  const canvas = document.getElementById("timelineCanvas");
  const container = document.getElementById("chartContainer");
  const elAvgCpm = document.getElementById("metricAvgCpm");
  const elPeakCpm = document.getElementById("metricPeakCpm");
  const elTotalTime = document.getElementById("metricTotalTime");

  if (!canvas || !container) return;

  const ctx = canvas.getContext("2d");
  const rect = container.getBoundingClientRect();
  const dpr = window.devicePixelRatio || 1;

  const width = rect.width > 0 ? rect.width : 1000;
  const height = rect.height > 0 ? rect.height : 300;
  canvas.width = width * dpr;
  canvas.height = height * dpr;
  ctx.resetTransform();
  ctx.scale(dpr, dpr);

  if (!timestamps || timestamps.length === 0) {
    ctx.clearRect(0, 0, width, height);
    ctx.fillStyle = "#64748b";
    ctx.font = "14px sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("타임라인 데이터가 없습니다.", width / 2, height / 2);
    if (elAvgCpm) elAvgCpm.innerText = "평균 타속: 0 CPM";
    if (elPeakCpm) elPeakCpm.innerText = "최고 타속: 0 CPM";
    if (elTotalTime) elTotalTime.innerText = "기록 시간: 0초";
    canvas.chartPoints = [];
    return;
  }

  // 1. 최대 시간(초) 및 인터벌 계산
  const maxElapsed = fixedMaxTime && fixedMaxTime > 0 ? fixedMaxTime : Math.max(...timestamps, 1);
  let interval = 5;
  if (currentIntervalSetting === "auto") {
    if (maxElapsed <= 30) interval = 1;
    else if (maxElapsed <= 75) interval = 2;
    else if (maxElapsed <= 240) interval = 5;
    else interval = 10;
  } else {
    interval = parseInt(currentIntervalSetting, 10) || 5;
  }

  // 2. 버킷 분할 및 카운팅
  const bucketCount = Math.max(Math.ceil(maxElapsed / interval), 1);
  const buckets = Array.from({ length: bucketCount }, (_, i) => ({
    start: i * interval,
    end: (i + 1) * interval,
    count: 0
  }));

  timestamps.forEach(t => {
    const idx = Math.min(Math.floor(t / interval), bucketCount - 1);
    if (idx >= 0 && idx < bucketCount) {
      buckets[idx].count++;
    }
  });

  // 평균 모드일 때: 세션당 평균 타수로 정규화
  if (isAverage && sessionCount > 1) {
    buckets.forEach(b => {
      b.count = Number((b.count / sessionCount).toFixed(1));
    });
  }

  // 3. 지표 산출
  const totalCount = isAverage && sessionCount > 1 ? timestamps.length / sessionCount : timestamps.length;
  const avgCpm = maxElapsed > 0 ? ((totalCount / maxElapsed) * 60).toFixed(0) : "0";
  const peakBucketCount = Math.max(...buckets.map(b => b.count), 0);
  const peakCpm = ((peakBucketCount / interval) * 60).toFixed(0);

  if (elAvgCpm) elAvgCpm.innerText = `평균 타속: ${Number(avgCpm).toLocaleString()} CPM`;
  if (elPeakCpm) elPeakCpm.innerText = `최고 타속: ${Number(peakCpm).toLocaleString()} CPM`;
  if (elTotalTime) elTotalTime.innerText = `기록 시간: ${maxElapsed.toFixed(1)}초`;

  // 4. 차트 레이아웃 좌표 설정
  const padding = { left: 56, right: 28, top: 26, bottom: 34 };
  const chartW = width - padding.left - padding.right;
  const chartH = height - padding.top - padding.bottom;

  ctx.clearRect(0, 0, width, height);

  // Y축 최댓값 계산 (여유율 1.15)
  const maxY = Math.max(peakBucketCount * 1.18, 4);

  // Y축 눈금선 (4단계)
  ctx.strokeStyle = "rgba(255, 255, 255, 0.08)";
  ctx.fillStyle = "#64748b";
  ctx.font = "11px monospace";
  ctx.textAlign = "right";
  ctx.lineWidth = 1;

  for (let i = 0; i <= 4; i++) {
    const yVal = (maxY / 4) * i;
    const yPos = padding.top + chartH - (yVal / maxY) * chartH;

    ctx.beginPath();
    ctx.moveTo(padding.left, yPos);
    ctx.lineTo(width - padding.right, yPos);
    ctx.stroke();

    const displayY = isAverage && yVal % 1 !== 0 ? yVal.toFixed(1) : Math.round(yVal);
    ctx.fillText(displayY, padding.left - 8, yPos + 4);
  }

  // X축 시간 라벨
  ctx.textAlign = "center";
  const labelInterval = Math.max(Math.ceil(bucketCount / 8), 1);
  for (let i = 0; i < bucketCount; i += labelInterval) {
    const xPos = padding.left + (i / (bucketCount - 1 || 1)) * chartW;
    const timeSec = Math.round(buckets[i].start);
    const m = Math.floor(timeSec / 60);
    const s = timeSec % 60;
    const timeLabel = m > 0 ? `${m}m ${s}s` : `${s}s`;

    ctx.fillText(timeLabel, xPos, height - padding.bottom + 20);
  }

  // 5. 막대 바 (Bar Chart)
  const barWidth = Math.max((chartW / bucketCount) * 0.65, 3);
  buckets.forEach((b, i) => {
    const xCenter = padding.left + (i / (bucketCount - 1 || 1)) * chartW;
    const bHeight = (b.count / maxY) * chartH;
    const yPos = padding.top + chartH - bHeight;

    const barGrad = ctx.createLinearGradient(0, yPos, 0, padding.top + chartH);
    barGrad.addColorStop(0, "rgba(99, 102, 241, 0.5)");
    barGrad.addColorStop(1, "rgba(99, 102, 241, 0.08)");

    ctx.fillStyle = barGrad;
    ctx.fillRect(xCenter - barWidth / 2, yPos, barWidth, bHeight);
  });

  // 6. 부드러운 스플라인 라인 & 영역 채우기
  const points = buckets.map((b, i) => ({
    x: padding.left + (i / (bucketCount - 1 || 1)) * chartW,
    y: padding.top + chartH - (b.count / maxY) * chartH,
    bucket: b
  }));

  // 영역 채우기 그라데이션
  const areaGrad = ctx.createLinearGradient(0, padding.top, 0, padding.top + chartH);
  areaGrad.addColorStop(0, "rgba(56, 189, 248, 0.3)");
  areaGrad.addColorStop(1, "rgba(56, 189, 248, 0.0)");

  ctx.beginPath();
  ctx.moveTo(points[0].x, padding.top + chartH);
  points.forEach((p, i) => {
    if (i === 0) {
      ctx.lineTo(p.x, p.y);
    } else {
      const prev = points[i - 1];
      const cx = (prev.x + p.x) / 2;
      ctx.bezierCurveTo(cx, prev.y, cx, p.y, p.x, p.y);
    }
  });
  ctx.lineTo(points[points.length - 1].x, padding.top + chartH);
  ctx.closePath();
  ctx.fillStyle = areaGrad;
  ctx.fill();

  // 라인(Line)
  ctx.beginPath();
  points.forEach((p, i) => {
    if (i === 0) {
      ctx.moveTo(p.x, p.y);
    } else {
      const prev = points[i - 1];
      const cx = (prev.x + p.x) / 2;
      ctx.bezierCurveTo(cx, prev.y, cx, p.y, p.x, p.y);
    }
  });
  ctx.strokeStyle = "#38bdf8";
  ctx.lineWidth = 2.5;
  ctx.shadowColor = "rgba(56, 189, 248, 0.8)";
  ctx.shadowBlur = 8;
  ctx.stroke();
  ctx.shadowBlur = 0; // 리셋

  // 점(Point) 표시
  points.forEach(p => {
    ctx.beginPath();
    ctx.arc(p.x, p.y, 3, 0, Math.PI * 2);
    ctx.fillStyle = "#ffffff";
    ctx.fill();
    ctx.strokeStyle = "#38bdf8";
    ctx.lineWidth = 2;
    ctx.stroke();
  });

  // 7. 마우스 호버 가이드라인 강조
  if (chartHoverData) {
    const hp = points[chartHoverData.index];
    if (hp) {
      // 세로 가이드라인 점선
      ctx.save();
      ctx.setLineDash([4, 4]);
      ctx.strokeStyle = "rgba(56, 189, 248, 0.8)";
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(hp.x, padding.top);
      ctx.lineTo(hp.x, padding.top + chartH);
      ctx.stroke();
      ctx.restore();

      // 호버 원 강조
      ctx.beginPath();
      ctx.arc(hp.x, hp.y, 6, 0, Math.PI * 2);
      ctx.fillStyle = "#38bdf8";
      ctx.fill();
      ctx.strokeStyle = "#ffffff";
      ctx.lineWidth = 2;
      ctx.stroke();
    }
  }

  // 캔버스 인터랙션 정보 바인딩
  canvas.chartPoints = points;
  canvas.chartInterval = interval;
  canvas.chartIsAverage = isAverage;
  canvas.chartSessionCount = sessionCount;
}

// 현재 세션 상태에 맞춰 차트 다시 그리기
function redrawCurrentChart() {
  if (loadedSessions.length === 0) {
    renderTimelineChart([], false, 0, 0);
    return;
  }
  if (loadedSessions.length === 1) {
    renderTimelineChart(loadedSessions[0].downTimestamps, false, 1, loadedSessions[0].maxElapsed);
    return;
  }
  if (selectedSessionIndex >= 0 && selectedSessionIndex < loadedSessions.length) {
    const session = loadedSessions[selectedSessionIndex];
    renderTimelineChart(session.downTimestamps, false, 1, session.maxElapsed);
    return;
  }
  const agg = getAggregatedData();
  const isAvg = currentViewMode === "average";
  renderTimelineChart(agg.downTimestamps, isAvg, agg.sessionCount, agg.maxElapsed);
}

// 10. 초기화(리셋) 함수
function resetViewer() {
  loadedSessions = [];
  selectedSessionIndex = -1;
  currentViewMode = "total";
  chartHoverData = null;

  const elSessionId = document.getElementById("sessionIdText");
  if (elSessionId) elSessionId.innerText = "Session: 대기 중 (CSV 파일을 로드하세요)";

  const elBuild = document.getElementById("buildVersionText");
  if (elBuild) elBuild.innerText = "Build: -";

  const elElapsed = document.getElementById("elapsedTimeText");
  if (elElapsed) elElapsed.innerText = "기록 시간: 0.00초";

  const elDown = document.getElementById("downCountText");
  if (elDown) elDown.innerText = "총 누름(Down): 0회";

  const elSummary = document.getElementById("summaryTotal");
  if (elSummary) elSummary.innerText = "총 Down 입력: 0회";

  const elTypeTotal = document.getElementById("typeTotalBadge");
  if (elTypeTotal) elTypeTotal.innerText = "총 Down 입력: 0회";

  const toolbar = document.getElementById("aggregateToolbar");
  if (toolbar) toolbar.style.display = "none";

  const fileCountBadge = document.getElementById("fileCountBadge");
  if (fileCountBadge) fileCountBadge.style.display = "none";

  const fileListTags = document.getElementById("fileListTags");
  if (fileListTags) {
    fileListTags.style.display = "none";
    fileListTags.innerHTML = "";
  }

  const sessionGlobalCard = document.getElementById("sessionGlobalCard");
  if (sessionGlobalCard) sessionGlobalCard.style.display = "none";

  const sessionTabs = document.getElementById("sessionTabs");
  if (sessionTabs) sessionTabs.innerHTML = "";

  const fileInput = document.getElementById("csvFileInput");
  if (fileInput) fileInput.value = "";

  const chartTooltip = document.getElementById("chartTooltip");
  if (chartTooltip) chartTooltip.style.display = "none";

  renderKeyboard({}, false, {}, 0);
  renderRanking({}, false);
  renderTimelineChart([], false, 0, 0);
}

// 복수 CSV 파일 읽기 및 세션 목록 업데이트
function loadFiles(fileList) {
  if (!fileList || fileList.length === 0) return;

  const csvFiles = Array.from(fileList).filter(f =>
    f.name.toLowerCase().endsWith(".csv") ||
    f.type.includes("csv") ||
    f.type.includes("text")
  );

  if (csvFiles.length === 0) {
    alert("선택된 파일 중 유효한 CSV 파일이 없습니다.");
    return;
  }

  const promises = csvFiles.map(file => {
    return new Promise(resolve => {
      const reader = new FileReader();
      reader.onload = event => {
        const parsed = parseSingleCsv(event.target.result, file.name);
        resolve(parsed);
      };
      reader.onerror = () => resolve(null);
      reader.readAsText(file, "UTF-8");
    });
  });

  Promise.all(promises).then(results => {
    const validSessions = results.filter(s => s !== null);
    if (validSessions.length === 0) {
      alert("CSV 데이터 파싱에 실패했습니다.");
      return;
    }

    // 파일 로드 시 기존 세션 대체
    loadedSessions = validSessions;
    selectedSessionIndex = -1;
    updateView();
  });
}

// 10. 초기화 및 이벤트 리스너 등록
document.addEventListener("DOMContentLoaded", () => {
  // 1) 기본 데이터로 첫 화면 렌더링
  const initialSession = parseSingleCsv(sampleCsvText, "sample_playtest_log.csv");
  if (initialSession) {
    loadedSessions = [initialSession];
    updateView();
  }

  // 2) 최신 Playtest 기본 로그 즉시 불러오기 버튼
  const loadDefaultBtn = document.getElementById("loadDefaultLogBtn");
  if (loadDefaultBtn) {
    loadDefaultBtn.addEventListener("click", () => {
      const defaultSession = parseSingleCsv(sampleCsvText, "sample_playtest_log.csv");
      if (defaultSession) {
        loadedSessions = [defaultSession];
        selectedSessionIndex = -1;
        updateView();
      }
    });
  }

  // 3) 리셋 버튼
  const resetBtn = document.getElementById("resetDataBtn");
  if (resetBtn) {
    resetBtn.addEventListener("click", () => {
      resetViewer();
    });
  }

  // 4) 파일 선택 input 리스너 (다중 파일 지원)
  const fileInput = document.getElementById("csvFileInput");
  if (fileInput) {
    fileInput.addEventListener("change", e => {
      loadFiles(e.target.files);
    });
  }

  // 5) 모드 탭 전환 버튼 (전체 종합 / 세션별 평균)
  const modeTotalBtn = document.getElementById("modeTotalBtn");
  const modeAverageBtn = document.getElementById("modeAverageBtn");

  if (modeTotalBtn) {
    modeTotalBtn.addEventListener("click", () => {
      currentViewMode = "total";
      selectedSessionIndex = -1;
      updateView();
    });
  }

  if (modeAverageBtn) {
    modeAverageBtn.addEventListener("click", () => {
      currentViewMode = "average";
      selectedSessionIndex = -1;
      updateView();
    });
  }

  // 6) 세션 선택 드롭다운 리스너
  const sessionSelect = document.getElementById("sessionSelect");
  if (sessionSelect) {
    sessionSelect.addEventListener("change", e => {
      const val = e.target.value;
      if (val === "ALL") {
        selectedSessionIndex = -1;
      } else {
        selectedSessionIndex = parseInt(val, 10);
      }
      updateView();
    });
  }

  // 7) 세션 글로벌 카드 네비게이션 탭 클릭
  const sessionTabs = document.getElementById("sessionTabs");
  if (sessionTabs) {
    sessionTabs.addEventListener("click", e => {
      const btn = e.target.closest(".player-tab-btn");
      if (!btn) return;
      const index = parseInt(btn.getAttribute("data-index"), 10);
      selectedSessionIndex = index;
      updateView();
    });
  }

  // 8) 이전(◀), 다음(▶) 세션 이동 버튼
  const prevSessionBtn = document.getElementById("prevSessionBtn");
  const nextSessionBtn = document.getElementById("nextSessionBtn");

  if (prevSessionBtn) {
    prevSessionBtn.addEventListener("click", () => {
      if (loadedSessions.length <= 1) return;
      if (selectedSessionIndex === -1) {
        selectedSessionIndex = loadedSessions.length - 1;
      } else if (selectedSessionIndex === 0) {
        selectedSessionIndex = -1; // 전체 통합으로 순환
      } else {
        selectedSessionIndex--;
      }
      updateView();
    });
  }

  if (nextSessionBtn) {
    nextSessionBtn.addEventListener("click", () => {
      if (loadedSessions.length <= 1) return;
      if (selectedSessionIndex === -1) {
        selectedSessionIndex = 0;
      } else if (selectedSessionIndex === loadedSessions.length - 1) {
        selectedSessionIndex = -1; // 전체 통합으로 순환
      } else {
        selectedSessionIndex++;
      }
      updateView();
    });
  }

  // 9) 파일 태그 개별 삭제 이벤트 위임
  const fileListTags = document.getElementById("fileListTags");
  if (fileListTags) {
    fileListTags.addEventListener("click", e => {
      const btn = e.target.closest(".tag-remove-btn");
      if (!btn) return;
      const index = parseInt(btn.getAttribute("data-index"), 10);
      if (!isNaN(index) && index >= 0 && index < loadedSessions.length) {
        loadedSessions.splice(index, 1);
        if (selectedSessionIndex === index) {
          selectedSessionIndex = -1;
        } else if (selectedSessionIndex > index) {
          selectedSessionIndex--;
        }
        updateView();
      }
    });
  }

  // 10) 드래그 앤 드롭 파일 로더 (복수 파일 동시 드롭 지원)
  window.addEventListener("dragover", e => {
    e.preventDefault();
  });

  window.addEventListener("drop", e => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      loadFiles(e.dataTransfer.files);
    }
  });

  // 11) 타임라인 차트 마우스 인터랙션 및 시간 단위 변경 리스너
  setupChartEvents();
});

// 차트 인터랙션 및 단위 변경 리스너 설정
function setupChartEvents() {
  const canvas = document.getElementById("timelineCanvas");
  const container = document.getElementById("chartContainer");
  const tooltip = document.getElementById("chartTooltip");
  const intervalSelect = document.getElementById("intervalSelect");

  if (!canvas || !container || !tooltip) return;

  canvas.addEventListener("mousemove", e => {
    const points = canvas.chartPoints;
    if (!points || points.length === 0) return;

    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;

    let closest = null;
    let minDist = Infinity;
    points.forEach((p, idx) => {
      const dist = Math.abs(p.x - mouseX);
      if (dist < minDist) {
        minDist = dist;
        closest = { p, idx };
      }
    });

    if (closest && minDist < 45) {
      const b = closest.p.bucket;
      const interval = canvas.chartInterval;
      const isAvg = canvas.chartIsAverage;
      const cpm = ((b.count / interval) * 60).toFixed(0);
      const countLabel = isAvg ? `평균 ${b.count}회` : `${b.count.toLocaleString()}회`;

      chartHoverData = { index: closest.idx };

      tooltip.style.display = "block";
      tooltip.style.left = closest.p.x + "px";
      tooltip.style.top = closest.p.y + "px";
      tooltip.innerHTML = `
        <div class="chart-tooltip-header">⏱️ ${b.start.toFixed(0)}초 ~ ${b.end.toFixed(0)}초 (${interval}초 구간)</div>
        <div class="chart-tooltip-body">
          <div>타격 횟수: <strong>${countLabel}</strong></div>
          <div>환산 타속: <strong>${Number(cpm).toLocaleString()} CPM</strong></div>
        </div>
      `;

      redrawCurrentChart();
    } else {
      if (chartHoverData) {
        chartHoverData = null;
        tooltip.style.display = "none";
        redrawCurrentChart();
      }
    }
  });

  canvas.addEventListener("mouseleave", () => {
    if (chartHoverData) {
      chartHoverData = null;
      tooltip.style.display = "none";
      redrawCurrentChart();
    }
  });

  if (intervalSelect) {
    intervalSelect.addEventListener("change", e => {
      currentIntervalSetting = e.target.value;
      redrawCurrentChart();
    });
  }

  window.addEventListener("resize", () => {
    redrawCurrentChart();
  });
}
