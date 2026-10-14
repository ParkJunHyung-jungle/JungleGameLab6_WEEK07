// 1. 실제 플레이테스트 CSV 데이터 (PlaytestLogs 로드본)
const sampleCsvText = `
session_id,build_version,sequence,record_type,elapsed_seconds,stage_id,stage_run_id,key,event,press_sequence,held_seconds,end_reason,key_down_count,interval_index,interval_kind,combo,start_elapsed_seconds,end_elapsed_seconds,duration_seconds,down_count,apm,player_score,enemy_score,player_score_start,player_score_end,score_delta,score_per_second,score_per_minute,is_complete
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,1,SessionStart,3.067741,,,,,,,,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,2,StageStart,5.642989,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,0,0,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,3,ScoreSnapshot,5.642989,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,0,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,4,KeyInput,5.857545,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,4,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,5,ScoreSnapshot,5.859103,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,10,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,6,KeyInput,5.860173,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,6,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,7,ScoreSnapshot,5.860235,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,20,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,8,KeyInput,5.892354,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Down,8,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,9,ScoreSnapshot,5.892419,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,30,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,10,KeyInput,5.892537,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,10,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,11,ScoreSnapshot,5.893075,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,40,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,12,KeyInput,5.893170,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,12,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,13,ScoreSnapshot,5.893214,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,50,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,14,KeyInput,5.912716,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,4,0.055172,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,15,KeyInput,5.974316,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,15,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,16,ScoreSnapshot,5.974389,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,60,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,17,KeyInput,5.974501,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,17,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,18,ScoreSnapshot,5.974535,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,70,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,19,KeyInput,5.974590,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,19,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,20,KeyInput,5.974609,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,19,0.000019,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,21,ScoreSnapshot,5.974636,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,80,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,22,KeyInput,5.974848,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,12,0.081677,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,23,KeyInput,5.974865,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,6,0.114692,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,24,KeyInput,5.974881,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Up,8,0.082527,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,25,KeyInput,5.992176,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,10,0.099639,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,26,KeyInput,5.992210,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,15,0.017894,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,27,KeyInput,6.021533,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,27,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,28,ScoreSnapshot,6.021611,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,90,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,29,KeyInput,6.021746,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Down,29,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,30,ScoreSnapshot,6.021790,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,100,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,31,KeyInput,6.041252,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,31,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,32,ScoreSnapshot,6.041318,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,110,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,33,KeyInput,6.041585,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,17,0.067084,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,34,KeyInput,6.068052,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,34,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,35,ScoreSnapshot,6.068122,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,120,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,36,KeyInput,6.068238,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,36,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,37,ScoreSnapshot,6.068271,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,130,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,38,KeyInput,6.068343,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,38,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,39,ScoreSnapshot,6.068380,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,140,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,40,KeyInput,6.093146,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,40,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,41,ScoreSnapshot,6.093209,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,150,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,42,KeyInput,6.093492,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,27,0.071960,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,43,KeyInput,6.093515,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,31,0.052263,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,44,KeyInput,6.093530,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,38,0.025187,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,45,KeyInput,6.093552,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Up,29,0.071805,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,46,KeyInput,6.123817,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,40,0.030671,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,47,KeyInput,6.145570,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,47,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,48,ScoreSnapshot,6.145659,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,160,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,49,KeyInput,6.148375,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,34,0.080322,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,50,KeyInput,6.148412,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,36,0.080173,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,51,KeyInput,6.170144,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Down,51,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,52,ScoreSnapshot,6.170218,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,170,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,53,KeyInput,6.170348,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,53,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,54,ScoreSnapshot,6.170389,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,180,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,55,KeyInput,6.170456,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,55,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,56,ScoreSnapshot,6.170484,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,190,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,57,KeyInput,6.195519,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,57,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,58,ScoreSnapshot,6.195603,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,200,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,59,KeyInput,6.195724,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,59,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,60,ScoreSnapshot,6.195760,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,210,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,61,KeyInput,6.203259,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,61,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,62,ScoreSnapshot,6.203325,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,220,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,63,KeyInput,6.217336,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,47,0.071766,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,64,KeyInput,6.235392,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,61,0.032133,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,65,KeyInput,6.248845,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,55,0.078389,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,66,KeyInput,6.248879,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,57,0.053361,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,67,KeyInput,6.248895,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,59,0.053172,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,68,KeyInput,6.248910,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Up,51,0.078766,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,69,KeyInput,6.269023,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,69,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,70,ScoreSnapshot,6.269091,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,230,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,71,KeyInput,6.269309,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Down,71,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,72,ScoreSnapshot,6.269355,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,240,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,73,KeyInput,6.269428,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,73,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,74,ScoreSnapshot,6.269459,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,250,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,75,KeyInput,6.298363,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,75,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,76,ScoreSnapshot,6.298427,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,260,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,77,KeyInput,6.298546,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,77,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,78,ScoreSnapshot,6.298581,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,270,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,79,KeyInput,6.305749,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,79,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,80,ScoreSnapshot,6.305822,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,280,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,81,KeyInput,6.324367,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,81,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,82,ScoreSnapshot,6.324444,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,290,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,83,KeyInput,6.324712,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,75,0.026349,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,84,KeyInput,6.348921,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,81,0.024554,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,85,KeyInput,6.373011,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,73,0.103582,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,86,KeyInput,6.398677,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,86,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,87,ScoreSnapshot,6.398748,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,300,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,88,KeyInput,6.402400,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,88,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,89,ScoreSnapshot,6.402467,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,310,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,90,KeyInput,6.443607,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,90,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,91,ScoreSnapshot,6.443670,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,320,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,92,KeyInput,6.444496,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,88,0.042096,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,93,KeyInput,6.465802,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,53,0.295454,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,94,KeyInput,6.465835,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,86,0.067158,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,95,KeyInput,6.492544,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,95,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,96,ScoreSnapshot,6.492611,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,330,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,97,KeyInput,6.492734,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,97,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,98,KeyInput,6.493013,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,90,0.049405,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,99,KeyInput,6.524093,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,99,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,100,ScoreSnapshot,6.524156,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,340,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,101,KeyInput,6.568958,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,101,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,102,KeyInput,6.569178,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,97,0.076444,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,103,KeyInput,6.596578,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,99,0.072485,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,104,KeyInput,6.652674,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,104,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,105,ScoreSnapshot,6.652738,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,350,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,106,KeyInput,6.653430,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,101,0.084472,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,107,KeyInput,6.673673,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,69,0.404650,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,108,KeyInput,6.673707,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,77,0.375161,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,109,KeyInput,6.699888,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,109,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,110,ScoreSnapshot,6.699961,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,360,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,111,KeyInput,6.720008,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,111,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,112,KeyInput,6.749248,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,112,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,113,KeyInput,6.749481,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,104,0.096807,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,114,KeyInput,6.771626,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,114,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,115,KeyInput,6.771850,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Up,71,0.502542,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,116,KeyInput,6.797485,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,95,0.304941,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,117,KeyInput,6.797523,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,111,0.077516,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,118,KeyInput,6.797542,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,109,0.097654,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,119,KeyInput,6.810372,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,79,0.504624,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,120,KeyInput,6.817273,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,112,0.068025,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,121,KeyInput,6.848229,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,114,0.076603,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,122,KeyInput,6.876101,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Down,122,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,123,ScoreSnapshot,6.876162,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,370,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,124,KeyInput,6.876261,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Down,124,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,125,ScoreSnapshot,6.876293,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,380,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,126,KeyInput,6.876366,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Down,126,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,127,ScoreSnapshot,6.876395,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,390,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,128,KeyInput,6.876443,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,128,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,129,ScoreSnapshot,6.876466,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,400,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,130,KeyInput,6.903140,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Up,122,0.027039,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,131,KeyInput,6.924922,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Down,131,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,132,ScoreSnapshot,6.924998,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,410,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,133,KeyInput,6.944143,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Up,126,0.067777,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,134,KeyInput,6.969585,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Up,124,0.093324,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,135,KeyInput,6.969630,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Up,131,0.044708,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,136,KeyInput,6.969648,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,128,0.093205,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,137,KeyInput,6.994992,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Down,137,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,138,ScoreSnapshot,6.995063,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,420,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,139,KeyInput,6.995193,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Down,139,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,140,ScoreSnapshot,6.995229,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,430,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,141,KeyInput,7.028187,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Tab,Down,141,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,142,ScoreSnapshot,7.028248,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,440,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,143,KeyInput,7.028355,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Down,143,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,144,ScoreSnapshot,7.028432,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,450,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,145,KeyInput,7.028476,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Down,145,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,146,ScoreSnapshot,7.028504,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,460,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,147,KeyInput,7.028594,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Down,147,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,148,ScoreSnapshot,7.028622,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,470,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,149,KeyInput,7.036575,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,149,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,150,ScoreSnapshot,7.036638,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,480,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,151,KeyInput,7.036747,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,151,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,152,ScoreSnapshot,7.036780,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,490,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,153,KeyInput,7.050053,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Up,143,0.021698,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,154,KeyInput,7.058737,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,151,0.021990,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,155,KeyInput,7.073599,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Tab,Up,141,0.045412,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,156,KeyInput,7.073638,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Up,145,0.045162,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,157,KeyInput,7.099318,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Down,157,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,158,ScoreSnapshot,7.099380,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,500,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,159,KeyInput,7.099623,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Up,137,0.104630,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,160,KeyInput,7.099644,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Up,139,0.104451,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,161,KeyInput,7.106567,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,149,0.069991,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,162,KeyInput,7.119229,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Down,162,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,163,ScoreSnapshot,7.119293,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,510,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,164,KeyInput,7.157477,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Down,164,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,165,ScoreSnapshot,7.157539,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,520,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,166,KeyInput,7.157650,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Down,166,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,167,ScoreSnapshot,7.157684,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,530,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,168,KeyInput,7.157765,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,168,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,169,ScoreSnapshot,7.157803,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,540,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,170,KeyInput,7.165221,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,170,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,171,ScoreSnapshot,7.165287,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,550,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,172,KeyInput,7.178762,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Down,172,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,173,ScoreSnapshot,7.178826,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,560,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,174,KeyInput,7.179087,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Up,157,0.079769,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,175,KeyInput,7.204269,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Up,164,0.046793,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,176,KeyInput,7.204302,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Up,166,0.046652,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,177,KeyInput,7.212835,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,170,0.047615,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,178,KeyInput,7.228032,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,168,0.070267,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,179,KeyInput,7.228065,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Up,172,0.049304,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,180,KeyInput,7.248282,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Down,180,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,181,ScoreSnapshot,7.248346,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,570,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,182,KeyInput,7.248472,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,182,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,183,ScoreSnapshot,7.248513,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,580,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,184,KeyInput,7.248745,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Up,162,0.129516,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,185,KeyInput,7.248816,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Up,147,0.220222,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,186,KeyInput,7.275253,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Down,186,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,187,ScoreSnapshot,7.275319,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,590,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,188,KeyInput,7.275436,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Down,188,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,189,ScoreSnapshot,7.275469,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,600,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,190,KeyInput,7.275536,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Down,190,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,191,ScoreSnapshot,7.275615,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,610,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,192,KeyInput,7.275691,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,192,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,193,ScoreSnapshot,7.275724,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,620,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,194,KeyInput,7.307326,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,192,0.031636,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,195,KeyInput,7.320313,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Up,180,0.072032,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,196,KeyInput,7.320346,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Up,190,0.044811,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,197,KeyInput,7.357597,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,182,0.109125,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,198,KeyInput,7.357633,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Up,186,0.082380,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,199,KeyInput,7.380051,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Down,199,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,200,ScoreSnapshot,7.380117,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,630,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,201,KeyInput,7.380406,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Up,188,0.104970,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,202,KeyInput,7.408311,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Down,202,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,203,ScoreSnapshot,7.408376,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,640,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,204,KeyInput,7.408595,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Down,204,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,205,ScoreSnapshot,7.408641,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,650,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,206,KeyInput,7.409118,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Down,206,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,207,ScoreSnapshot,7.409172,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,660,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,208,KeyInput,7.409523,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Down,208,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,209,ScoreSnapshot,7.409573,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,670,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,210,KeyInput,7.409899,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,210,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,211,ScoreSnapshot,7.409945,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,680,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,212,KeyInput,7.410246,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,212,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,213,ScoreSnapshot,7.410297,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,690,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,214,KeyInput,7.410575,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Up,199,0.030523,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,215,KeyInput,7.425290,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Down,215,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,216,ScoreSnapshot,7.425356,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,700,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,217,KeyInput,7.451075,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,212,0.040829,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,218,KeyInput,7.451108,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Up,202,0.042797,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,219,KeyInput,7.475706,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Up,208,0.066183,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,220,KeyInput,7.495028,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Down,220,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,221,ScoreSnapshot,7.495092,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,710,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,222,KeyInput,7.530247,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Down,222,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,223,ScoreSnapshot,7.530310,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,720,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,224,KeyInput,7.531043,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,210,0.121144,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,225,KeyInput,7.581388,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Down,225,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,226,KeyInput,7.581453,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,226,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,227,KeyInput,7.581654,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Up,220,0.086626,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,228,KeyInput,7.624811,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Up,222,0.094565,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,229,KeyInput,7.656148,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,226,0.074695,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,230,KeyInput,7.656182,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Up,225,0.074794,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,231,KeyInput,7.685815,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Down,231,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,232,ScoreSnapshot,7.685881,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,730,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,233,KeyInput,7.686199,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Up,204,0.277604,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,234,KeyInput,7.705735,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Up,206,0.296617,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,235,KeyInput,7.757651,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Down,235,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,236,KeyInput,7.757753,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Down,236,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,237,KeyInput,7.757949,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Up,231,0.072134,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,238,KeyInput,7.764915,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Up,215,0.339625,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,239,KeyInput,7.784929,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Down,239,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,240,KeyInput,7.784995,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,240,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,241,KeyInput,7.803929,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Down,241,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,242,KeyInput,7.823678,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Down,242,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,243,ScoreSnapshot,7.823743,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,740,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,244,KeyInput,7.823996,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Up,235,0.066345,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,245,KeyInput,7.852863,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Up,236,0.095111,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,246,KeyInput,7.852897,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Y,Up,241,0.048969,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,247,KeyInput,7.872390,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Down,247,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,248,ScoreSnapshot,7.872462,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,750,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,249,KeyInput,7.872980,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,249,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,250,ScoreSnapshot,7.873045,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,760,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,251,KeyInput,7.916864,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Down,251,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,252,KeyInput,7.917088,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Up,242,0.093409,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,253,KeyInput,7.917117,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,240,0.132122,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,254,KeyInput,7.925464,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Down,254,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,255,KeyInput,7.951446,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Down,255,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,256,ScoreSnapshot,7.951512,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,770,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,257,KeyInput,7.952146,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,249,0.079166,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,258,KeyInput,7.965297,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,W,Up,254,0.039833,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,259,KeyInput,7.977645,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Q,Up,251,0.060782,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,260,KeyInput,8.001157,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,E,Up,239,0.216228,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,261,KeyInput,8.001194,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,T,Up,247,0.128804,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,262,KeyInput,8.032913,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Down,262,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,263,ScoreSnapshot,8.032979,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,780,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,264,KeyInput,8.033083,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Down,264,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,265,ScoreSnapshot,8.033120,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,790,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,266,KeyInput,8.033336,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Up,255,0.081890,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,267,KeyInput,8.058155,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit8,Down,267,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,268,ScoreSnapshot,8.058218,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,800,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,269,KeyInput,8.108141,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Down,269,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,270,ScoreSnapshot,8.108213,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,810,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,271,KeyInput,8.108321,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Down,271,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,272,ScoreSnapshot,8.108354,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,820,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,273,KeyInput,8.108406,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Down,273,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,274,ScoreSnapshot,8.108439,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,830,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,275,KeyInput,8.108668,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit8,Up,267,0.050513,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,276,KeyInput,8.127733,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Up,269,0.019592,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,277,KeyInput,8.127768,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Up,264,0.094685,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,278,KeyInput,8.150939,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Down,278,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,279,ScoreSnapshot,8.151003,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,840,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,280,KeyInput,8.211691,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Down,280,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,281,ScoreSnapshot,8.211756,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,850,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,282,KeyInput,8.212110,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Up,271,0.103789,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,283,KeyInput,8.212140,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Up,278,0.061201,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,284,KeyInput,8.212164,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Up,273,0.103758,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,285,KeyInput,8.231089,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Down,285,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,286,ScoreSnapshot,8.231157,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,860,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,287,KeyInput,8.256799,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Down,287,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,288,ScoreSnapshot,8.256869,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,870,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,289,KeyInput,8.256978,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Down,289,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,290,ScoreSnapshot,8.257011,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,880,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,291,KeyInput,8.282167,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Down,291,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,292,ScoreSnapshot,8.282231,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,890,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,293,KeyInput,8.282559,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Up,262,0.249646,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,294,KeyInput,8.306387,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Down,294,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,295,ScoreSnapshot,8.306451,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,900,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,296,KeyInput,8.306760,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Up,285,0.075671,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,297,KeyInput,8.306782,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Up,287,0.049984,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,298,KeyInput,8.306797,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Up,280,0.095106,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,299,KeyInput,8.331384,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Up,291,0.049217,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,300,KeyInput,8.356807,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Down,300,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,301,ScoreSnapshot,8.356868,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,910,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,302,KeyInput,8.356983,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Down,302,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,303,ScoreSnapshot,8.357018,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,920,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,304,KeyInput,8.378512,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Down,304,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,305,ScoreSnapshot,8.378577,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,930,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,306,KeyInput,8.378697,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Down,306,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,307,ScoreSnapshot,8.378730,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,940,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,308,KeyInput,8.378805,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Down,308,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,309,ScoreSnapshot,8.378835,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,950,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,310,KeyInput,8.378916,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit8,Down,310,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,311,ScoreSnapshot,8.378945,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,960,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,312,KeyInput,8.379167,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Up,289,0.122190,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,313,KeyInput,8.379186,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Up,294,0.072799,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,314,KeyInput,8.436403,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Down,314,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,315,ScoreSnapshot,8.436463,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,970,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,316,KeyInput,8.436777,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Up,300,0.079971,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,317,KeyInput,8.436800,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit8,Up,310,0.057884,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,318,KeyInput,8.459692,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Up,304,0.081180,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,319,KeyInput,8.459724,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Up,308,0.080918,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,320,KeyInput,8.485126,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Down,320,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,321,ScoreSnapshot,8.485197,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,980,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,322,KeyInput,8.485517,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Up,302,0.128534,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,323,KeyInput,8.505460,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Down,323,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,324,ScoreSnapshot,8.505524,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,990,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,325,KeyInput,8.505648,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Down,325,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,326,ScoreSnapshot,8.505684,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1000,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,327,KeyInput,8.536746,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Down,327,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,328,ScoreSnapshot,8.536809,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1010,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,329,KeyInput,8.544047,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Down,329,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,330,ScoreSnapshot,8.544111,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1020,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,331,KeyInput,8.586343,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Up,320,0.101218,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,332,KeyInput,8.586384,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Up,327,0.049637,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,333,KeyInput,8.608330,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Up,323,0.102870,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,334,KeyInput,8.608363,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Up,325,0.102714,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,335,KeyInput,8.608378,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Up,306,0.229681,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,336,KeyInput,8.635001,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Up,314,0.198598,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,337,KeyInput,8.660697,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Down,337,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,338,ScoreSnapshot,8.660767,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1030,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,339,KeyInput,8.660886,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Down,339,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,340,ScoreSnapshot,8.660920,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1040,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,341,KeyInput,8.661461,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Down,341,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,342,ScoreSnapshot,8.661523,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1050,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,343,KeyInput,8.661631,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Down,343,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,344,ScoreSnapshot,8.661668,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1060,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,345,KeyInput,8.706306,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Down,345,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,346,ScoreSnapshot,8.706375,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1070,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,347,KeyInput,8.706685,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Up,337,0.045988,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,348,KeyInput,8.737650,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Up,345,0.031344,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,349,KeyInput,8.737719,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Up,343,0.076088,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,350,KeyInput,8.759188,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Up,339,0.098301,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,351,KeyInput,8.759220,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Up,341,0.097759,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,352,KeyInput,8.768188,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Up,329,0.224141,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,353,KeyInput,8.789226,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Down,353,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,354,ScoreSnapshot,8.789296,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1080,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,355,KeyInput,8.789835,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Down,355,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,356,KeyInput,8.789888,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Down,356,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,357,ScoreSnapshot,8.789926,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1090,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,358,KeyInput,8.790287,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Down,358,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,359,ScoreSnapshot,8.790335,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1100,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,360,KeyInput,8.790682,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Down,360,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,361,ScoreSnapshot,8.790730,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1110,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,362,KeyInput,8.810943,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Down,362,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,363,ScoreSnapshot,8.811011,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1120,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,364,KeyInput,8.818612,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Down,364,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,365,ScoreSnapshot,8.818674,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1130,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,366,KeyInput,8.832081,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Up,362,0.021137,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,367,KeyInput,8.857068,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Up,356,0.067180,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,368,KeyInput,8.881214,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Up,358,0.090927,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,369,KeyInput,8.914260,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Down,369,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,370,KeyInput,8.937614,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Down,370,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,371,KeyInput,8.937756,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit8,Down,371,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,372,ScoreSnapshot,8.937797,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1140,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,373,KeyInput,8.963373,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Down,373,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,374,ScoreSnapshot,8.963439,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1150,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,375,KeyInput,8.988729,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit8,Up,371,0.050973,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,376,KeyInput,9.007862,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit1,Up,353,0.218637,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,377,KeyInput,9.007902,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit2,Up,355,0.218067,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,378,KeyInput,9.039214,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit3,Up,369,0.124954,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,379,KeyInput,9.039248,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit4,Up,370,0.101633,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,380,KeyInput,9.039263,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit7,Up,360,0.248581,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,381,KeyInput,9.052307,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit6,Up,364,0.233695,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,382,KeyInput,9.067498,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Digit5,Up,373,0.104125,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,383,KeyInput,9.136706,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Down,383,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,384,ScoreSnapshot,9.136770,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1160,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,385,KeyInput,9.136882,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Down,385,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,386,ScoreSnapshot,9.136918,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1170,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,387,KeyInput,9.163517,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Down,387,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,388,ScoreSnapshot,9.163581,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1180,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,389,KeyInput,9.163701,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Down,389,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,390,ScoreSnapshot,9.163738,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1190,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,391,KeyInput,9.163782,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Down,391,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,392,ScoreSnapshot,9.163808,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1200,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,393,KeyInput,9.163843,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Down,393,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,394,ScoreSnapshot,9.163866,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1210,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,395,KeyInput,9.171397,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Down,395,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,396,ScoreSnapshot,9.171458,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1220,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,397,KeyInput,9.171537,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Down,397,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,398,ScoreSnapshot,9.171567,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1230,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,399,KeyInput,9.190685,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Up,383,0.053979,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,400,KeyInput,9.203505,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Up,397,0.031969,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,401,KeyInput,9.210899,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Up,385,0.074017,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,402,KeyInput,9.210930,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Up,387,0.047413,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,403,KeyInput,9.210946,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Up,389,0.047245,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,404,KeyInput,9.210960,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Up,391,0.047178,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,405,KeyInput,9.234615,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Up,393,0.070772,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,406,KeyInput,9.247470,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Up,395,0.076073,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,407,KeyInput,9.290347,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Down,407,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,408,ScoreSnapshot,9.290409,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1240,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,409,KeyInput,9.290520,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Down,409,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,410,ScoreSnapshot,9.290552,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1250,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,411,KeyInput,9.321546,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Down,411,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,412,ScoreSnapshot,9.321681,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1260,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,413,KeyInput,9.321823,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Down,413,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,414,ScoreSnapshot,9.321864,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1270,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,415,KeyInput,9.321927,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Down,415,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,416,ScoreSnapshot,9.321962,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1280,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,417,KeyInput,9.322014,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Down,417,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,418,ScoreSnapshot,9.322046,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1290,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,419,KeyInput,9.329743,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Down,419,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,420,ScoreSnapshot,9.329805,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1300,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,421,KeyInput,9.329922,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Down,421,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,422,ScoreSnapshot,9.329981,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1310,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,423,KeyInput,9.337433,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Up,407,0.047085,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,424,KeyInput,9.362832,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Up,409,0.072313,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,425,KeyInput,9.362866,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Up,411,0.041320,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,426,KeyInput,9.362886,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Up,417,0.040871,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,427,KeyInput,9.380221,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Up,421,0.050299,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,428,KeyInput,9.387542,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Up,413,0.065719,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,429,KeyInput,9.387583,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Up,415,0.065656,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,430,KeyInput,9.394383,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Up,419,0.064640,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,431,KeyInput,9.444945,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Down,431,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,432,ScoreSnapshot,9.445007,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1320,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,433,KeyInput,9.445127,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Down,433,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,434,ScoreSnapshot,9.445239,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1330,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,435,KeyInput,9.445308,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Down,435,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,436,ScoreSnapshot,9.445342,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1340,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,437,KeyInput,9.464528,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Down,437,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,438,ScoreSnapshot,9.464596,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1350,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,439,KeyInput,9.464724,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Down,439,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,440,ScoreSnapshot,9.464770,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1360,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,441,KeyInput,9.464831,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Down,441,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,442,ScoreSnapshot,9.464861,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1370,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,443,KeyInput,9.472163,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Down,443,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,444,ScoreSnapshot,9.472224,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1380,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,445,KeyInput,9.491345,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Down,445,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,446,ScoreSnapshot,9.491419,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1390,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,447,KeyInput,9.511533,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Up,431,0.066588,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,448,KeyInput,9.511566,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Up,433,0.066439,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,449,KeyInput,9.511581,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Up,435,0.066273,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,450,KeyInput,9.511595,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Up,437,0.047067,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,451,KeyInput,9.511608,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Up,439,0.046884,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,452,KeyInput,9.518959,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Up,445,0.027614,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,453,KeyInput,9.536406,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Up,441,0.071576,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,454,KeyInput,9.572246,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Up,443,0.100083,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,455,KeyInput,9.592192,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Down,455,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,456,ScoreSnapshot,9.592255,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1400,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,457,KeyInput,9.592443,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Down,457,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,458,ScoreSnapshot,9.592484,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1410,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,459,KeyInput,9.592541,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Down,459,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,460,ScoreSnapshot,9.592568,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1420,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,461,KeyInput,9.592628,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Down,461,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,462,ScoreSnapshot,9.592655,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1430,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,463,KeyInput,9.618118,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Down,463,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,464,ScoreSnapshot,9.618179,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1440,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,465,KeyInput,9.618299,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Down,465,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,466,ScoreSnapshot,9.618331,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1450,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,467,KeyInput,9.625479,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Down,467,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,468,ScoreSnapshot,9.625543,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1460,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,469,KeyInput,9.644819,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Down,469,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,470,ScoreSnapshot,9.644885,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1470,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,471,KeyInput,9.645233,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Up,455,0.053041,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,472,KeyInput,9.664920,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Up,457,0.072477,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,473,KeyInput,9.664954,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Up,459,0.072414,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,474,KeyInput,9.664983,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Up,461,0.072355,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,475,KeyInput,9.664999,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Up,469,0.020180,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,476,KeyInput,9.672181,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Up,467,0.046702,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,477,KeyInput,9.714865,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Down,477,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,478,ScoreSnapshot,9.714932,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1480,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,479,KeyInput,9.715286,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Down,479,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,480,ScoreSnapshot,9.715416,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1490,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,481,KeyInput,9.715660,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Down,481,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,482,ScoreSnapshot,9.715701,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1500,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,483,KeyInput,9.715879,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Down,483,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,484,ScoreSnapshot,9.715914,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1510,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,485,KeyInput,9.716311,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Up,463,0.098194,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,486,KeyInput,9.736451,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Up,465,0.118152,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,487,KeyInput,9.763150,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Down,487,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,488,ScoreSnapshot,9.763214,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1520,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,489,KeyInput,9.763560,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Down,489,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,490,ScoreSnapshot,9.763605,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1530,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,491,KeyInput,9.763795,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Down,491,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,492,ScoreSnapshot,9.763831,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1540,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,493,KeyInput,9.763990,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Down,493,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,494,ScoreSnapshot,9.764023,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1550,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,495,KeyInput,9.764418,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Up,477,0.049553,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,496,KeyInput,9.764445,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Up,479,0.049159,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,497,KeyInput,9.790105,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Up,481,0.074445,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,498,KeyInput,9.790137,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Up,483,0.074258,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,499,KeyInput,9.815226,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Up,487,0.052076,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,500,KeyInput,9.815262,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Up,493,0.051272,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,501,KeyInput,9.845127,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Down,501,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,502,KeyInput,9.845263,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Down,502,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,503,KeyInput,9.845528,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Up,489,0.081969,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,504,KeyInput,9.864454,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Down,504,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,505,KeyInput,9.891747,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Down,505,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,506,KeyInput,9.922765,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Down,506,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,507,KeyInput,9.929396,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Down,507,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,508,KeyInput,9.943725,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Down,508,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,509,KeyInput,9.943991,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Up,501,0.098864,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,510,KeyInput,9.967167,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F6,Up,508,0.023442,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,511,KeyInput,9.994030,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Down,511,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,512,KeyInput,10.017636,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Up,504,0.153183,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,513,KeyInput,10.017670,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F7,Up,491,0.253875,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,514,KeyInput,10.040347,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Up,506,0.117581,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,515,KeyInput,10.040378,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F8,Up,505,0.148632,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,516,KeyInput,10.063161,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Down,516,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,517,KeyInput,10.080243,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F5,Up,507,0.150847,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,518,KeyInput,10.087003,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Down,518,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,519,KeyInput,10.087283,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F1,Up,511,0.093253,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,520,KeyInput,10.120302,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F2,Up,502,0.275039,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,521,KeyInput,10.168760,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F3,Up,516,0.105599,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,522,KeyInput,10.190965,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F4,Up,518,0.103962,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,523,KeyInput,10.218777,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Down,523,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,524,ScoreSnapshot,10.218841,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1560,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,525,KeyInput,10.265993,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Up,523,0.047216,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,526,KeyInput,10.368645,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Down,526,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,527,ScoreSnapshot,10.368707,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1570,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,528,KeyInput,10.392804,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Down,528,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,529,ScoreSnapshot,10.392885,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1580,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,530,KeyInput,10.452658,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Up,528,0.059854,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,531,KeyInput,10.470799,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Up,526,0.102154,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,532,KeyInput,10.495064,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Down,532,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,533,ScoreSnapshot,10.495130,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1590,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,534,KeyInput,10.545749,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Down,534,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,535,ScoreSnapshot,10.545812,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1600,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,536,KeyInput,10.545954,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Down,536,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,537,ScoreSnapshot,10.546000,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1610,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,538,KeyInput,10.546084,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F12,Down,538,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,539,ScoreSnapshot,10.546118,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1620,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,540,KeyInput,10.606635,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Up,534,0.060886,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,541,KeyInput,10.606668,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F12,Up,538,0.060584,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,542,KeyInput,10.619567,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Up,536,0.073614,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,543,KeyInput,10.694793,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Down,543,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,544,ScoreSnapshot,10.694856,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1630,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,545,KeyInput,10.694990,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Down,545,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,546,ScoreSnapshot,10.695027,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1640,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,547,KeyInput,10.751181,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Up,532,0.256117,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,548,KeyInput,10.766831,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Up,543,0.072037,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,549,KeyInput,10.826276,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Down,549,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,550,ScoreSnapshot,10.826341,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1650,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,551,KeyInput,10.826480,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F12,Down,551,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,552,ScoreSnapshot,10.826519,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1660,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,553,KeyInput,10.874820,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Up,545,0.179830,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,554,KeyInput,10.874860,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F12,Up,551,0.048380,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,555,KeyInput,10.926651,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Up,549,0.100375,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,556,KeyInput,10.951946,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F12,Down,556,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,557,ScoreSnapshot,10.952012,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1670,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,558,KeyInput,10.976831,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Down,558,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,559,ScoreSnapshot,10.976895,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1680,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,560,KeyInput,10.977066,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Down,560,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,561,ScoreSnapshot,10.977108,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1690,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,562,KeyInput,10.977187,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Down,562,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,563,ScoreSnapshot,10.977223,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1700,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,564,KeyInput,11.002641,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Up,558,0.025810,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,565,KeyInput,11.052611,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Up,560,0.075544,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,566,KeyInput,11.052668,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Up,562,0.075481,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,567,KeyInput,11.052689,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F12,Up,556,0.100742,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,568,KeyInput,11.073451,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Down,568,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,569,ScoreSnapshot,11.073513,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1710,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,570,KeyInput,11.144404,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Down,570,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,571,KeyInput,11.205766,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Up,568,0.132315,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,572,KeyInput,11.226555,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Down,572,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,573,ScoreSnapshot,11.226616,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1720,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,574,KeyInput,11.226973,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,574,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,575,KeyInput,11.274797,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Down,575,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,576,ScoreSnapshot,11.274879,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1730,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,577,KeyInput,11.305943,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Down,577,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,578,ScoreSnapshot,11.306009,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1740,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,579,KeyInput,11.306510,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,574,0.079537,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,580,KeyInput,11.331251,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,580,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,581,ScoreSnapshot,11.331320,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1750,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,582,KeyInput,11.331589,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Up,572,0.105035,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,583,KeyInput,11.350878,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Up,575,0.076081,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,584,KeyInput,11.350923,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Up,570,0.206518,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,585,KeyInput,11.426956,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Down,585,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,586,KeyInput,11.427015,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Down,586,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,587,KeyInput,11.427096,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Down,587,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,588,ScoreSnapshot,11.427136,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1760,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,589,KeyInput,11.452622,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Up,580,0.121371,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,590,KeyInput,11.452669,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Up,577,0.146726,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,591,KeyInput,11.506788,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,591,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,592,ScoreSnapshot,11.506850,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1770,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,593,KeyInput,11.507122,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Up,587,0.080026,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,594,KeyInput,11.530519,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Down,594,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,595,KeyInput,11.530763,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F10,Up,585,0.103807,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,596,KeyInput,11.554482,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F11,Up,586,0.127467,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,597,KeyInput,11.578714,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Down,597,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,598,ScoreSnapshot,11.578780,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1780,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,599,KeyInput,11.579127,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F9,Up,594,0.048609,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,600,KeyInput,11.626476,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Up,591,0.119688,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,601,KeyInput,11.650353,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,601,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,602,ScoreSnapshot,11.650416,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1790,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,603,KeyInput,11.679221,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Up,597,0.100507,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,604,KeyInput,11.726857,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,604,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,605,ScoreSnapshot,11.726919,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1800,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,606,KeyInput,11.751128,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Up,601,0.100775,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,607,KeyInput,11.776355,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Down,607,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,608,ScoreSnapshot,11.776418,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1810,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,609,KeyInput,11.776528,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Down,609,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,610,ScoreSnapshot,11.776565,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1820,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,611,KeyInput,11.776798,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,604,0.049942,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,612,KeyInput,11.834645,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Down,612,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,613,ScoreSnapshot,11.834711,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1830,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,614,KeyInput,11.834814,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,614,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,615,ScoreSnapshot,11.834845,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1840,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,616,KeyInput,11.853279,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Up,607,0.076924,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,617,KeyInput,11.883067,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Up,609,0.106539,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,618,KeyInput,11.908833,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,618,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,619,ScoreSnapshot,11.908906,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1850,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,620,KeyInput,11.909025,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Down,620,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,621,ScoreSnapshot,11.909146,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1860,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,622,KeyInput,11.909366,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Up,614,0.074552,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,623,KeyInput,11.909390,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Up,612,0.074745,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,624,KeyInput,11.928025,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,624,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,625,ScoreSnapshot,11.928087,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1870,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,626,KeyInput,11.976355,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,626,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,627,ScoreSnapshot,11.976424,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1880,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,628,KeyInput,11.976747,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,628,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,629,KeyInput,11.976801,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Down,629,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,630,ScoreSnapshot,11.976840,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1890,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,631,KeyInput,11.976896,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Down,631,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,632,ScoreSnapshot,11.976922,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1900,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,633,KeyInput,11.977151,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,624,0.049126,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,634,KeyInput,11.977172,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,618,0.068339,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,635,KeyInput,12.010192,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,635,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,636,KeyInput,12.010403,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Up,620,0.101378,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,637,KeyInput,12.010425,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Up,631,0.033529,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,638,KeyInput,12.034282,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Up,629,0.057481,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,639,KeyInput,12.058779,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Down,639,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,640,ScoreSnapshot,12.058842,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1910,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,641,KeyInput,12.059095,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,626,0.082740,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,642,KeyInput,12.059116,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,628,0.082369,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,643,KeyInput,12.078612,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,643,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,644,KeyInput,12.078759,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,644,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,645,ScoreSnapshot,12.078806,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1920,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,646,KeyInput,12.079114,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,635,0.068922,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,647,KeyInput,12.103652,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,647,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,648,KeyInput,12.139661,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Down,648,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,649,ScoreSnapshot,12.139733,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1930,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,650,KeyInput,12.140087,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,650,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,651,KeyInput,12.140305,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Up,639,0.081526,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,652,KeyInput,12.140333,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,643,0.061721,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,653,KeyInput,12.154028,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,653,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,654,KeyInput,12.154250,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,644,0.075491,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,655,KeyInput,12.207196,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Up,648,0.067535,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,656,KeyInput,12.231277,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,656,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,657,KeyInput,12.231490,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,647,0.127838,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,658,KeyInput,12.268080,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,658,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,659,KeyInput,12.268143,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,659,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,660,ScoreSnapshot,12.268195,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1940,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,661,KeyInput,12.268467,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,650,0.128380,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,662,KeyInput,12.268488,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,653,0.114459,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,663,KeyInput,12.315112,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,663,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,664,ScoreSnapshot,12.315176,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1950,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,665,KeyInput,12.315528,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,665,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,666,KeyInput,12.315777,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,658,0.047697,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,667,KeyInput,12.335143,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,667,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,668,KeyInput,12.335362,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,659,0.067219,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,669,KeyInput,12.360103,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,669,,,10,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,670,KeyInput,12.360329,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,656,0.129052,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,671,KeyInput,12.385780,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,671,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,672,KeyInput,12.385979,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,663,0.070867,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,673,KeyInput,12.410159,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,665,0.094631,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,674,KeyInput,12.431694,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,674,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,675,KeyInput,12.431758,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,O,Down,675,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,676,ScoreSnapshot,12.431792,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1960,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,677,KeyInput,12.432070,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,667,0.096927,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,678,KeyInput,12.454608,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,678,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,679,KeyInput,12.454815,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,669,0.094712,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,680,KeyInput,12.454835,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,671,0.069055,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,681,KeyInput,12.501117,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,681,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,682,KeyInput,12.536173,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,O,Up,675,0.104415,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,683,KeyInput,12.560482,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,683,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,684,KeyInput,12.560675,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,678,0.106067,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,685,KeyInput,12.560695,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,674,0.129001,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,686,KeyInput,12.605468,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,686,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,687,KeyInput,12.633251,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,681,0.132134,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,688,KeyInput,12.633292,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,683,0.072810,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,689,KeyInput,12.656051,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,689,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,690,KeyInput,12.656112,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,O,Down,690,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,691,ScoreSnapshot,12.656148,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1970,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,692,KeyInput,12.685906,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Down,692,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,693,KeyInput,12.709950,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,686,0.104482,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,694,KeyInput,12.736950,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,694,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,695,KeyInput,12.737130,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,689,0.081079,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,696,KeyInput,12.737151,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Up,692,0.051245,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,697,KeyInput,12.757254,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,O,Up,690,0.101142,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,698,KeyInput,12.786601,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,698,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,699,KeyInput,12.832987,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,699,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,700,KeyInput,12.833051,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,O,Down,700,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,701,ScoreSnapshot,12.833086,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1980,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,702,KeyInput,12.862186,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,694,0.125237,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,703,KeyInput,12.887645,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,698,0.101045,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,704,KeyInput,12.913107,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Down,704,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,705,KeyInput,12.937005,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,699,0.104017,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,706,KeyInput,12.937040,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,O,Up,700,0.103989,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,707,KeyInput,12.962975,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Down,707,,,10,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,708,KeyInput,12.963047,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,708,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,709,KeyInput,12.963177,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Up,704,0.050070,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,710,KeyInput,13.066958,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Down,710,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,711,KeyInput,13.067030,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,O,Down,711,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,712,ScoreSnapshot,13.067065,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,1990,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,713,KeyInput,13.067342,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,708,0.104295,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,714,KeyInput,13.067367,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,U,Up,707,0.104392,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,715,KeyInput,13.085221,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Down,715,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,716,KeyInput,13.109041,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,I,Up,715,0.023820,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,717,KeyInput,13.109076,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,R,Up,710,0.042118,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,718,KeyInput,13.137745,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,O,Up,711,0.070715,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,719,KeyInput,13.161033,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Down,719,,,10,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,720,KeyInput,13.238949,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,A,Up,719,0.077916,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,721,KeyInput,13.287730,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,721,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,722,ScoreSnapshot,13.287795,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2000,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,723,KeyInput,13.288099,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Down,723,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,724,ScoreSnapshot,13.288148,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2010,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,725,KeyInput,13.333521,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Up,723,0.045422,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,726,KeyInput,13.368765,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,721,0.081034,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,727,KeyInput,13.438339,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,727,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,728,KeyInput,13.438401,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,728,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,729,KeyInput,13.487266,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,729,,,11,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,730,KeyInput,13.514568,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,730,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,731,KeyInput,13.514644,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Down,731,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,732,ScoreSnapshot,13.514680,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2020,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,733,KeyInput,13.514907,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,727,0.076568,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,734,KeyInput,13.544161,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,728,0.105760,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,735,KeyInput,13.567841,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,729,0.080575,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,736,KeyInput,13.591154,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Up,731,0.076510,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,737,KeyInput,13.615336,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,737,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,738,KeyInput,13.615404,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Down,738,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,739,ScoreSnapshot,13.615441,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2030,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,740,KeyInput,13.615683,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,730,0.101115,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,741,KeyInput,13.640498,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,741,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,742,KeyInput,13.640577,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Down,742,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,743,ScoreSnapshot,13.640613,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2040,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,744,KeyInput,13.640750,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,744,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,745,ScoreSnapshot,13.640789,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2050,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,746,KeyInput,13.669682,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Up,742,0.029105,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,747,KeyInput,13.694264,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Down,747,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,748,ScoreSnapshot,13.694332,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2060,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,749,KeyInput,13.694426,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Down,749,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,750,ScoreSnapshot,13.694458,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2070,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,751,KeyInput,13.694669,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Up,744,0.053919,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,752,KeyInput,13.694701,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Up,738,0.079297,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,753,KeyInput,13.770151,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,753,,,10,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,754,KeyInput,13.770356,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,741,0.129858,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,755,KeyInput,13.770376,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,737,0.155040,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,756,KeyInput,13.770393,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Up,747,0.076129,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,757,KeyInput,13.815888,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Down,757,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,758,ScoreSnapshot,13.815958,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2080,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,759,KeyInput,13.840876,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,759,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,760,KeyInput,13.841093,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Up,749,0.146667,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,761,KeyInput,13.841115,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,753,0.070964,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,762,KeyInput,13.865000,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Down,762,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,763,ScoreSnapshot,13.865066,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2090,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,764,KeyInput,13.917872,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Down,764,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,765,ScoreSnapshot,13.917938,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2100,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,766,KeyInput,13.918240,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Comma,Down,766,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,767,ScoreSnapshot,13.918279,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2110,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,768,KeyInput,13.918487,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Up,757,0.102599,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,769,KeyInput,13.942804,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,M,Down,769,,,1,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,770,ScoreSnapshot,13.942870,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2120,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,771,KeyInput,13.943115,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Up,759,0.102239,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,772,KeyInput,13.967538,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Up,762,0.102539,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,773,KeyInput,13.991373,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Up,764,0.073501,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,774,KeyInput,13.991407,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,M,Up,769,0.048603,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,775,KeyInput,14.015528,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Down,775,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,776,ScoreSnapshot,14.015593,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2130,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,777,KeyInput,14.015907,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Down,777,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,778,ScoreSnapshot,14.015949,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2140,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,779,KeyInput,14.016275,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Comma,Up,766,0.098035,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,780,KeyInput,14.073793,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Down,780,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,781,ScoreSnapshot,14.073859,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2150,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,782,KeyInput,14.092306,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Up,775,0.076778,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,783,KeyInput,14.122146,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Down,783,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,784,KeyInput,14.122214,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,M,Down,784,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,785,ScoreSnapshot,14.122254,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2160,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,786,KeyInput,14.122396,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Comma,Down,786,,,2,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,787,ScoreSnapshot,14.122433,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2170,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,788,KeyInput,14.122632,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Up,777,0.106725,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,789,KeyInput,14.167491,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Up,780,0.093698,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,790,KeyInput,14.197636,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Down,790,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,791,KeyInput,14.197706,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Down,791,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,792,KeyInput,14.197880,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Up,783,0.075734,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,793,KeyInput,14.216510,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,793,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,794,KeyInput,14.216726,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Comma,Up,786,0.094330,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,795,KeyInput,14.216757,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,M,Up,784,0.094543,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,796,KeyInput,14.245807,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Down,796,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,797,KeyInput,14.274601,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Down,797,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,798,KeyInput,14.274804,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Up,793,0.058294,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,799,KeyInput,14.274826,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Up,790,0.077190,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,800,KeyInput,14.292986,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Up,796,0.047178,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,801,KeyInput,14.321087,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Comma,Down,801,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,802,ScoreSnapshot,14.321150,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2180,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,803,KeyInput,14.321410,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Up,797,0.046810,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,804,KeyInput,14.321436,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,N,Up,791,0.123730,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,805,KeyInput,14.368221,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Down,805,,,3,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,806,ScoreSnapshot,14.368292,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2190,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,807,KeyInput,14.368450,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Down,807,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,808,KeyInput,14.368491,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,808,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,809,KeyInput,14.425566,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Comma,Up,801,0.104479,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,810,KeyInput,14.425611,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Up,808,0.057120,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,811,KeyInput,14.425635,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Up,807,0.057186,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,812,KeyInput,14.425659,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Up,805,0.057438,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,813,KeyInput,14.448935,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Down,813,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,814,KeyInput,14.496329,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,H,Up,813,0.047393,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,815,KeyInput,14.522360,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,815,,,11,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,816,KeyInput,14.522428,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,816,,,10,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,817,KeyInput,14.522501,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,817,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,818,KeyInput,14.522533,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Down,818,,,4,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,819,ScoreSnapshot,14.522567,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2200,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,820,KeyInput,14.522702,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Down,820,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,821,KeyInput,14.540669,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,821,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,822,KeyInput,14.573550,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,822,,,12,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,823,KeyInput,14.573761,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,821,0.033092,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,824,KeyInput,14.573786,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,X,Up,820,0.051084,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,825,KeyInput,14.573800,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Up,818,0.051267,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,826,KeyInput,14.599609,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,815,0.077249,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,827,KeyInput,14.599642,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,816,0.077214,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,828,KeyInput,14.599657,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,817,0.077156,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,829,KeyInput,14.623575,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,829,,,10,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,830,KeyInput,14.646930,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,822,0.073380,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,831,KeyInput,14.676625,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,831,,,12,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,832,KeyInput,14.676697,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Down,832,,,5,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,833,ScoreSnapshot,14.676731,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2210,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,834,KeyInput,14.677117,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,829,0.053542,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,835,KeyInput,14.695378,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,835,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,836,KeyInput,14.695554,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,831,0.018929,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,837,KeyInput,14.720524,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,837,,,13,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,838,KeyInput,14.720587,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,838,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,839,KeyInput,14.750410,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,839,,,11,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,840,KeyInput,14.750511,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,840,,,11,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,841,KeyInput,14.750665,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,835,0.055287,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,842,KeyInput,14.750688,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Up,832,0.073990,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,843,KeyInput,14.774469,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,837,0.053946,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,844,KeyInput,14.797734,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,840,0.047223,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,845,KeyInput,14.797774,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,838,0.077188,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,846,KeyInput,14.826065,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,839,0.075656,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,847,KeyInput,14.849956,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,847,,,10,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,848,KeyInput,14.850030,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Down,848,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,849,KeyInput,14.894534,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,849,,,14,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,850,KeyInput,14.894600,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,850,,,12,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,851,KeyInput,14.894638,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,851,,,10,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,852,KeyInput,14.894779,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Up,848,0.044748,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,853,KeyInput,14.916472,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,847,0.066517,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,854,KeyInput,14.949210,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,854,,,12,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,855,KeyInput,14.949383,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,849,0.054849,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,856,KeyInput,14.972374,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Down,856,,,13,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,857,KeyInput,14.972543,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,850,0.077943,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,858,KeyInput,15.000049,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,858,,,11,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,859,KeyInput,15.000217,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,854,0.051007,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,860,KeyInput,15.000239,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,851,0.105601,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,861,KeyInput,15.047816,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Down,861,,,15,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,862,KeyInput,15.081257,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Down,862,,,13,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,863,KeyInput,15.081323,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Down,863,,,13,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,864,KeyInput,15.081360,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Down,864,,,11,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,865,KeyInput,15.081484,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,858,0.081435,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,866,KeyInput,15.104071,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,F,Up,861,0.056255,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,867,KeyInput,15.104107,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,J,Up,856,0.131733,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,868,KeyInput,15.125477,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,K,Up,863,0.044154,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,869,KeyInput,15.152145,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,G,Up,862,0.070888,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,870,KeyInput,15.152190,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,L,Up,864,0.070830,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,871,KeyInput,15.174816,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Down,871,,,6,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,872,KeyInput,15.174879,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Down,872,,,12,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,873,KeyInput,15.174915,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Down,873,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,874,KeyInput,15.205971,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,S,Up,871,0.031155,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,875,KeyInput,15.206013,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Up,873,0.031098,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,876,KeyInput,15.221475,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,D,Up,872,0.046597,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,877,KeyInput,15.548179,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Down,877,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,878,KeyInput,15.574925,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,C,Down,878,,,9,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,879,KeyInput,15.574987,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,V,Down,879,,,7,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,880,KeyInput,15.632314,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,B,Down,880,,,8,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,881,KeyInput,15.632485,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,Z,Up,877,0.084306,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,882,ScoreSnapshot,15.646640,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,,,,,,,,,,,2210,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,883,IntervalSummary,15.646640,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,TimeOut,,0,BeforeFirstCombo,0,5.642989,15.646640,10.003651,330,1979.277445,,,0,2210,2210,220.919351,13255.161071,true
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,884,StageEnd,15.646640,Assets/Scenes/Week7/ManagerMerge.unity/TimedRound,57dd17d8b00c4ad1a39781193107a856,,,,,TimeOut,,,,,5.642989,15.646640,10.003651,,,2210,780,,,,,,true
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,885,KeyInput,15.673129,,,C,Up,878,0.098204,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,886,KeyInput,15.699645,,,V,Up,879,0.124658,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,887,KeyInput,15.716838,,,B,Up,880,0.084524,Released,,,,,,,,,,,,,,,,,
5bad0978e51b4ad2b30f4676ff125e95,0.1.0,888,SessionEnd,20.258009,,,,,,,ApplicationQuit,,,,,,,,,,,,,,,,,true`;

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

// RFC 4180 호환 CSV 한 줄 분리 함수
function parseCsvLine(line) {
  const result = [];
  let current = "";
  let inQuotes = false;
  for (let i = 0; i < line.length; i++) {
    const char = line[i];
    if (char === '"') {
      if (inQuotes && line[i + 1] === '"') {
        current += '"';
        i++;
      } else {
        inQuotes = !inQuotes;
      }
    } else if (char === ',' && !inQuotes) {
      result.push(current.trim());
      current = "";
    } else {
      current += char;
    }
  }
  result.push(current.trim());
  return result;
}

// 단일 CSV 텍스트 파싱 (신규 29열 및 기존 구버전 하위 호환)
function parseSingleCsv(csvContent, fileName = "Unknown") {
  if (!csvContent || csvContent.trim().length === 0) return null;

  const lines = csvContent.trim().split(/\r?\n/);
  if (lines.length < 2) return null;

  const header = parseCsvLine(lines[0]).map(h => h.trim().toLowerCase());
  const col = {
    sessionId: header.indexOf("session_id"),
    buildVersion: header.indexOf("build_version"),
    sequence: header.indexOf("sequence"),
    recordType: header.indexOf("record_type"),
    elapsedSeconds: header.indexOf("elapsed_seconds"),
    stageId: header.indexOf("stage_id"),
    stageRunId: header.indexOf("stage_run_id"),
    key: header.indexOf("key"),
    event: header.indexOf("event"),
    pressSequence: header.indexOf("press_sequence"),
    heldSeconds: header.indexOf("held_seconds"),
    endReason: header.indexOf("end_reason"),
    keyDownCount: header.indexOf("key_down_count"),
    intervalIndex: header.indexOf("interval_index"),
    intervalKind: header.indexOf("interval_kind"),
    combo: header.indexOf("combo"),
    startElapsed: header.indexOf("start_elapsed_seconds"),
    endElapsed: header.indexOf("end_elapsed_seconds"),
    duration: header.indexOf("duration_seconds"),
    downCount: header.indexOf("down_count"),
    apm: header.indexOf("apm"),
    playerScore: header.indexOf("player_score"),
    enemyScore: header.indexOf("enemy_score"),
    isComplete: header.indexOf("is_complete")
  };

  let sessionId = "-";
  let buildVersion = "-";
  let stageId = "-";
  let stageRunId = "-";
  let maxElapsed = 0;
  let minElapsed = Infinity;
  let totalDownCount = 0;
  const keyCounts = {};
  const downTimestamps = [];
  const scoreSnapshots = [];
  let finalPlayerScore = null;
  let finalEnemyScore = null;
  let stageDuration = 0;
  let endReason = "-";
  let isComplete = false;
  let apmList = [];
  let maxCombo = 0;

  for (let i = 1; i < lines.length; i++) {
    const rawLine = lines[i].trim();
    if (!rawLine) continue;
    const row = parseCsvLine(rawLine);
    if (row.length < 3) continue;

    const rowSessionId = col.sessionId !== -1 ? row[col.sessionId] : "";
    const rowBuildVersion = col.buildVersion !== -1 ? row[col.buildVersion] : "";
    const rowRecordType = col.recordType !== -1 ? row[col.recordType] : "";
    const rawKey = col.key !== -1 ? row[col.key] : "";
    const event = col.event !== -1 ? row[col.event].toLowerCase() : "";
    const elapsed = col.elapsedSeconds !== -1 ? parseFloat(row[col.elapsedSeconds]) : NaN;
    const rowStageId = col.stageId !== -1 ? row[col.stageId] : "";
    const rowStageRunId = col.stageRunId !== -1 ? row[col.stageRunId] : "";
    const rowPlayerScore = col.playerScore !== -1 ? row[col.playerScore] : "";
    const rowEnemyScore = col.enemyScore !== -1 ? row[col.enemyScore] : "";
    const rowApm = col.apm !== -1 ? row[col.apm] : "";
    const rowCombo = col.combo !== -1 ? row[col.combo] : "";
    const rowEndReason = col.endReason !== -1 ? row[col.endReason] : "";
    const rowDuration = col.duration !== -1 ? row[col.duration] : "";
    const rowIsComplete = col.isComplete !== -1 ? row[col.isComplete] : "";

    if (rowSessionId && sessionId === "-") sessionId = rowSessionId;
    if (rowBuildVersion && buildVersion === "-") buildVersion = rowBuildVersion;
    if (rowStageId && stageId === "-") stageId = rowStageId;
    if (rowStageRunId && stageRunId === "-") stageRunId = rowStageRunId;

    if (!isNaN(elapsed)) {
      if (elapsed > maxElapsed) maxElapsed = elapsed;
      if (elapsed < minElapsed) minElapsed = elapsed;
    }

    // 1) 레코드 타입 기반 처리 (신규 29열 CSV)
    if (col.recordType !== -1 && rowRecordType) {
      if (rowRecordType === "KeyInput" && event === "down") {
        const normalizedKey = normalizeUnityKey(rawKey);
        if (normalizedKey) {
          keyCounts[normalizedKey] = (keyCounts[normalizedKey] || 0) + 1;
          totalDownCount++;
          if (!isNaN(elapsed) && elapsed >= 0) {
            downTimestamps.push(elapsed);
          }
        }
      } else if (rowRecordType === "ScoreSnapshot") {
        if (rowPlayerScore !== "") {
          const score = parseInt(rowPlayerScore, 10);
          if (!isNaN(score)) {
            finalPlayerScore = score;
            if (!isNaN(elapsed)) {
              scoreSnapshots.push({ elapsed, score });
            }
          }
        }
      } else if (rowRecordType === "IntervalSummary") {
        if (rowApm !== "") {
          const apmVal = parseFloat(rowApm);
          if (!isNaN(apmVal)) apmList.push(apmVal);
        }
        if (rowCombo !== "") {
          const comboVal = parseInt(rowCombo, 10);
          if (!isNaN(comboVal) && comboVal > maxCombo) maxCombo = comboVal;
        }
      } else if (rowRecordType === "StageStart") {
        if (rowPlayerScore !== "") finalPlayerScore = parseInt(rowPlayerScore, 10);
        if (rowEnemyScore !== "") finalEnemyScore = parseInt(rowEnemyScore, 10);
      } else if (rowRecordType === "StageEnd") {
        if (rowPlayerScore !== "") finalPlayerScore = parseInt(rowPlayerScore, 10);
        if (rowEnemyScore !== "") finalEnemyScore = parseInt(rowEnemyScore, 10);
        if (rowEndReason) endReason = rowEndReason;
        if (rowDuration !== "") stageDuration = parseFloat(rowDuration);
        if (rowIsComplete.toLowerCase() === "true") isComplete = true;
      }
    } else {
      // 2) 레코드 타입이 없는 구버전 CSV (하위 호환)
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
  }

  if (sessionId === "-") sessionId = fileName.replace(/\.csv$/i, "");
  if (minElapsed === Infinity) minElapsed = 0;
  const playDuration = stageDuration > 0 ? stageDuration : (maxElapsed - (minElapsed > 0 && minElapsed < maxElapsed ? minElapsed : 0));

  // 스테이지 이름 정리 (예: Assets/Scenes/.../TimedRound -> TimedRound)
  let cleanStageName = "-";
  if (stageId !== "-") {
    const parts = stageId.split(/[\/\\]/);
    cleanStageName = parts[parts.length - 1] || stageId;
  }

  // APM 산출
  let avgApm = 0;
  if (apmList.length > 0) {
    avgApm = apmList.reduce((a, b) => a + b, 0) / apmList.length;
  } else if (playDuration > 0 && totalDownCount > 0) {
    avgApm = (totalDownCount * 60) / playDuration;
  }

  // 승패 판정
  let result = "UNKNOWN"; // "WIN" | "LOSE" | "DRAW" | "UNKNOWN"
  if (finalPlayerScore !== null && finalEnemyScore !== null) {
    if (finalPlayerScore > finalEnemyScore) result = "WIN";
    else if (finalPlayerScore < finalEnemyScore) result = "LOSE";
    else result = "DRAW";
  }

  return {
    fileName,
    sessionId,
    stageId: cleanStageName,
    fullStageId: stageId,
    stageRunId,
    buildVersion,
    maxElapsed,
    minElapsed,
    playDuration,
    totalDownCount,
    keyCounts,
    downTimestamps,
    scoreSnapshots,
    finalPlayerScore: finalPlayerScore !== null ? finalPlayerScore : 0,
    finalEnemyScore: finalEnemyScore !== null ? finalEnemyScore : 0,
    hasScoreData: finalPlayerScore !== null,
    result,
    endReason,
    isComplete,
    avgApm: Math.round(avgApm),
    maxCombo
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
      downTimestamps: [],
      avgPlayerScore: 0,
      maxPlayerScore: 0,
      totalPlayerScore: 0,
      winCount: 0,
      loseCount: 0,
      winRate: 0,
      avgApm: 0,
      hasScoreData: false
    };
  }

  const count = loadedSessions.length;
  let totalDown = 0;
  let totalElapsed = 0;
  let overallMaxElapsed = 0;
  const versions = new Set();
  const totalKeys = {};
  let allTimestamps = [];
  let totalScore = 0;
  let maxScore = 0;
  let winCount = 0;
  let loseCount = 0;
  let totalApm = 0;
  let scoreDataCount = 0;

  loadedSessions.forEach(session => {
    totalDown += session.totalDownCount;
    const dur = session.playDuration > 0 ? session.playDuration : session.maxElapsed;
    totalElapsed += dur;
    if (session.maxElapsed > overallMaxElapsed) overallMaxElapsed = session.maxElapsed;
    if (session.buildVersion && session.buildVersion !== "-") versions.add(session.buildVersion);

    for (const [key, val] of Object.entries(session.keyCounts)) {
      totalKeys[key] = (totalKeys[key] || 0) + val;
    }
    if (session.downTimestamps && session.downTimestamps.length > 0) {
      allTimestamps = allTimestamps.concat(session.downTimestamps);
    }

    if (session.hasScoreData) {
      scoreDataCount++;
      totalScore += session.finalPlayerScore;
      if (session.finalPlayerScore > maxScore) maxScore = session.finalPlayerScore;
      if (session.result === "WIN") winCount++;
      else if (session.result === "LOSE") loseCount++;
    }
    totalApm += session.avgApm || 0;
  });

  const avgKeys = {};
  for (const [key, val] of Object.entries(totalKeys)) {
    avgKeys[key] = Number((val / count).toFixed(2));
  }

  const avgDown = totalDown / count;
  const avgElapsed = totalElapsed / count;
  const avgPlayerScore = scoreDataCount > 0 ? Math.round(totalScore / scoreDataCount) : 0;
  const winRate = scoreDataCount > 0 ? Math.round((winCount / scoreDataCount) * 100) : 0;
  const avgApm = Math.round(totalApm / count);

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
    downTimestamps: allTimestamps,
    avgPlayerScore,
    maxPlayerScore: maxScore,
    totalPlayerScore: totalScore,
    winCount,
    loseCount,
    winRate,
    avgApm,
    hasScoreData: scoreDataCount > 0
  };
}

// 다중 세션 플레이 성과 비교 테이블 렌더링
function renderComparisonTable() {
  const card = document.getElementById("comparisonCard");
  const tbody = document.getElementById("comparisonTableBody");
  const badge = document.getElementById("comparisonCountBadge");

  if (!card || !tbody) return;

  if (loadedSessions.length <= 1) {
    card.style.display = "none";
    return;
  }

  card.style.display = "flex";
  if (badge) badge.innerText = `${loadedSessions.length}개 세션 로드됨`;

  tbody.innerHTML = "";
  loadedSessions.forEach((session, idx) => {
    const tr = document.createElement("tr");
    tr.className = "comparison-row" + (selectedSessionIndex === idx ? " active" : "");
    tr.title = "클릭하여 이 세션 상세 분석 보기";

    // 결과 배지
    let resultBadge = `<span class="badge badge-draw">-</span>`;
    if (session.result === "WIN") {
      resultBadge = `<span class="badge badge-win">승리 🏆</span>`;
    } else if (session.result === "LOSE") {
      resultBadge = `<span class="badge badge-lose">패배 💀</span>`;
    } else if (session.result === "DRAW") {
      resultBadge = `<span class="badge badge-draw">무승부</span>`;
    }

    const durationStr = session.playDuration > 0 ? session.playDuration.toFixed(1) + "초" : session.maxElapsed.toFixed(1) + "초";
    const playerScoreStr = session.hasScoreData ? session.finalPlayerScore.toLocaleString() + "점" : "-";
    const enemyScoreStr = session.hasScoreData ? session.finalEnemyScore.toLocaleString() + "점" : "-";
    const stageStr = session.stageId !== "-" ? `<span class="badge badge-stage">${session.stageId}</span>` : "-";

    tr.innerHTML = `
      <td><strong>${idx + 1}</strong></td>
      <td class="table-file-cell">
        <span>📄</span>
        <span class="table-file-name" title="${session.fileName}">${session.fileName}</span>
      </td>
      <td>${stageStr}</td>
      <td><span class="badge badge-score">${playerScoreStr}</span></td>
      <td>${enemyScoreStr}</td>
      <td>${resultBadge}</td>
      <td><strong>${session.totalDownCount.toLocaleString()}</strong>회</td>
      <td><span class="badge badge-apm">${session.avgApm > 0 ? session.avgApm.toLocaleString() + " APM" : "-"}</span></td>
      <td>${durationStr}</td>
      <td>
        <button type="button" class="table-action-btn">
          ${selectedSessionIndex === idx ? "✓ 조회 중" : "분석"}
        </button>
      </td>
    `;

    tr.addEventListener("click", () => {
      selectedSessionIndex = idx;
      updateView();
    });

    tbody.appendChild(tr);
  });
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

  // 신규 메트릭 표시 엘리먼트
  const stageModeText = document.getElementById("stageModeText");
  const scoreText = document.getElementById("scoreText");
  const apmText = document.getElementById("apmText");
  const summaryStatsGrid = document.getElementById("summaryStatsGrid");
  const statAvgScore = document.getElementById("statAvgScore");
  const statMaxScore = document.getElementById("statMaxScore");
  const statWinRate = document.getElementById("statWinRate");
  const statAvgApm = document.getElementById("statAvgApm");

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
    if (summaryStatsGrid) summaryStatsGrid.style.display = "none";

    const session = loadedSessions[0];
    if (elSessionId) elSessionId.innerText = "Session: " + session.sessionId;
    if (elBuild) elBuild.innerText = "Build: " + session.buildVersion;
    if (elElapsed) elElapsed.innerText = "기록 시간: " + (session.playDuration > 0 ? session.playDuration.toFixed(2) : session.maxElapsed.toFixed(2)) + "초";
    if (elDown) elDown.innerText = "총 누름(Down): " + session.totalDownCount.toLocaleString() + "회";
    if (elSummary) elSummary.innerText = "총 Down 입력: " + session.totalDownCount.toLocaleString() + "회";
    if (elTypeTotal) elTypeTotal.innerText = "총 Down 입력: " + session.totalDownCount.toLocaleString() + "회";

    if (stageModeText) {
      if (session.stageId !== "-") {
        stageModeText.style.display = "inline";
        stageModeText.innerText = "스테이지: " + session.stageId;
      } else {
        stageModeText.style.display = "none";
      }
    }

    if (scoreText) {
      if (session.hasScoreData) {
        scoreText.style.display = "inline";
        const resultEmoji = session.result === "WIN" ? " 🏆 (승리)" : (session.result === "LOSE" ? " 💀 (패배)" : "");
        scoreText.innerText = `점수: ${session.finalPlayerScore.toLocaleString()}점 (적: ${session.finalEnemyScore.toLocaleString()}점${resultEmoji})`;
      } else {
        scoreText.style.display = "none";
      }
    }

    if (apmText) {
      if (session.avgApm > 0) {
        apmText.style.display = "inline";
        apmText.innerText = `평균 APM: ${session.avgApm.toLocaleString()}`;
      } else {
        apmText.style.display = "none";
      }
    }

    renderComparisonTable();
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
      const scorePart = s.hasScoreData ? `, ${s.finalPlayerScore.toLocaleString()}점` : "";
      opt.innerText = `${idx + 1}. ${s.fileName} (${s.sessionId.substring(0, 8)}...${scorePart})`;
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

  const agg = getAggregatedData();
  const isAvg = currentViewMode === "average";

  // 다중 세션 핵심 메트릭 카드 갱신
  if (summaryStatsGrid) {
    summaryStatsGrid.style.display = "grid";
    if (statAvgScore) statAvgScore.innerText = agg.hasScoreData ? `${agg.avgPlayerScore.toLocaleString()}점` : "-";
    if (statMaxScore) statMaxScore.innerText = agg.hasScoreData ? `${agg.maxPlayerScore.toLocaleString()}점` : "-";
    if (statWinRate) statWinRate.innerText = agg.hasScoreData ? `${agg.winRate}% (${agg.winCount}승 ${agg.loseCount}패)` : "-";
    if (statAvgApm) statAvgApm.innerText = agg.avgApm > 0 ? `${agg.avgApm.toLocaleString()} APM` : "-";
  }

  // 비교 테이블 렌더링
  renderComparisonTable();

  // 1) 특정 개별 세션 조회 중인 경우
  if (selectedSessionIndex >= 0 && selectedSessionIndex < loadedSessions.length) {
    const session = loadedSessions[selectedSessionIndex];
    if (elSessionId) elSessionId.innerText = `[${selectedSessionIndex + 1}/${loadedSessions.length}] Session: ${session.sessionId}`;
    if (elBuild) elBuild.innerText = "Build: " + session.buildVersion;
    if (elElapsed) elElapsed.innerText = "기록 시간: " + (session.playDuration > 0 ? session.playDuration.toFixed(2) : session.maxElapsed.toFixed(2)) + "초";
    if (elDown) elDown.innerText = "총 누름(Down): " + session.totalDownCount.toLocaleString() + "회";
    if (elSummary) elSummary.innerText = "개별 Down 입력: " + session.totalDownCount.toLocaleString() + "회";
    if (elTypeTotal) elTypeTotal.innerText = "개별 Down 입력: " + session.totalDownCount.toLocaleString() + "회";

    if (stageModeText) {
      if (session.stageId !== "-") {
        stageModeText.style.display = "inline";
        stageModeText.innerText = "스테이지: " + session.stageId;
      } else {
        stageModeText.style.display = "none";
      }
    }

    if (scoreText) {
      if (session.hasScoreData) {
        scoreText.style.display = "inline";
        const resultEmoji = session.result === "WIN" ? " 🏆 (승리)" : (session.result === "LOSE" ? " 💀 (패배)" : "");
        scoreText.innerText = `점수: ${session.finalPlayerScore.toLocaleString()}점 (적: ${session.finalEnemyScore.toLocaleString()}점${resultEmoji})`;
      } else {
        scoreText.style.display = "none";
      }
    }

    if (apmText) {
      if (session.avgApm > 0) {
        apmText.style.display = "inline";
        apmText.innerText = `평균 APM: ${session.avgApm.toLocaleString()}`;
      } else {
        apmText.style.display = "none";
      }
    }

    if (currentSessionAvatar) currentSessionAvatar.innerText = session.result === "WIN" ? "🏆" : "🎮";
    if (currentSessionTag) currentSessionTag.innerText = `INDIVIDUAL SESSION #${selectedSessionIndex + 1} (${session.stageId})`;
    if (currentSessionBadge) currentSessionBadge.innerText = `Session ${selectedSessionIndex + 1}`;
    if (currentSessionName) {
      const scoreStr = session.hasScoreData ? `, ${session.finalPlayerScore.toLocaleString()}점` : "";
      currentSessionName.innerText = `${session.fileName} (기록 ${(session.playDuration > 0 ? session.playDuration : session.maxElapsed).toFixed(1)}초, ${session.totalDownCount.toLocaleString()}타${scoreStr})`;
    }

    renderKeyboard(session.keyCounts, false, session.keyCounts, 1);
    renderRanking(session.keyCounts, false);
    renderTimelineChart(session.downTimestamps, false, 1, session.maxElapsed);
    return;
  }

  // 2) 전체 다중 세션 종합 또는 평균 조회 중인 경우
  if (stageModeText) stageModeText.style.display = "none";

  if (scoreText) {
    if (agg.hasScoreData) {
      scoreText.style.display = "inline";
      scoreText.innerText = isAvg ? `평균 점수: ${agg.avgPlayerScore.toLocaleString()}점` : `총 누적 점수: ${agg.totalPlayerScore.toLocaleString()}점`;
    } else {
      scoreText.style.display = "none";
    }
  }

  if (apmText) {
    if (agg.avgApm > 0) {
      apmText.style.display = "inline";
      apmText.innerText = `평균 APM: ${agg.avgApm.toLocaleString()}`;
    } else {
      apmText.style.display = "none";
    }
  }

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

  const stageModeText = document.getElementById("stageModeText");
  if (stageModeText) stageModeText.style.display = "none";

  const scoreText = document.getElementById("scoreText");
  if (scoreText) scoreText.style.display = "none";

  const apmText = document.getElementById("apmText");
  if (apmText) apmText.style.display = "none";

  const summaryStatsGrid = document.getElementById("summaryStatsGrid");
  if (summaryStatsGrid) summaryStatsGrid.style.display = "none";

  const comparisonCard = document.getElementById("comparisonCard");
  if (comparisonCard) comparisonCard.style.display = "none";

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

// 복수 CSV 파일 읽기 및 세션 목록 업데이트 (누적 추가 지원)
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

    // 파일 로드 시: 기존에 샘플 1개만 로드되어 있던 상태면 대체, 아니면 기존 세션에 누적 추가
    const isSampleOnly = loadedSessions.length === 1 && loadedSessions[0].fileName === "sample_playtest_log.csv";
    if (isSampleOnly) {
      loadedSessions = validSessions;
    } else {
      // 기존 목록과 중복(동일 세션ID 및 파일명) 제거 후 추가
      validSessions.forEach(newSession => {
        const existingIdx = loadedSessions.findIndex(s => s.sessionId === newSession.sessionId && s.fileName === newSession.fileName);
        if (existingIdx >= 0) {
          loadedSessions[existingIdx] = newSession; // 기존 것 갱신
        } else {
          loadedSessions.push(newSession);
        }
      });
    }

    selectedSessionIndex = -1; // 전체 통합 뷰로 리셋
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

  // 12) PDF 인쇄/저장 버튼 리스너 등록
  setupPdfExportEvents();

  // 13) 이미지(PNG) 저장 버튼 리스너 등록
  setupImageExportEvents();
});

// PDF 내보내기 함수 및 이벤트 핸들러
function exportPdf(targetView) {
  const classNames = ["print-target-view2", "print-target-view3", "print-target-view4", "print-target-all"];
  document.body.classList.remove(...classNames);

  if (targetView === "view2") {
    document.body.classList.add("print-target-view2");
  } else if (targetView === "view3") {
    document.body.classList.add("print-target-view3");
  } else if (targetView === "view4") {
    document.body.classList.add("print-target-view4");
  } else {
    document.body.classList.add("print-target-all");
  }

  // 캔버스 크기 재조정 후 인쇄 창 호출
  redrawCurrentChart();

  setTimeout(() => {
    window.print();
  }, 120);
}

// 인쇄 창 닫힌 후 인쇄용 전용 클래스 정리
window.addEventListener("afterprint", () => {
  document.body.classList.remove("print-target-view2", "print-target-view3", "print-target-view4", "print-target-all");
  redrawCurrentChart();
});

function setupPdfExportEvents() {
  const btnView2 = document.getElementById("printView2Btn");
  if (btnView2) {
    btnView2.addEventListener("click", () => exportPdf("view2"));
  }

  const btnView3 = document.getElementById("printView3Btn");
  if (btnView3) {
    btnView3.addEventListener("click", () => exportPdf("view3"));
  }

  const btnView4 = document.getElementById("printView4Btn");
  if (btnView4) {
    btnView4.addEventListener("click", () => exportPdf("view4"));
  }

  const btnAll = document.getElementById("printAllViewsBtn");
  if (btnAll) {
    btnAll.addEventListener("click", () => exportPdf("all"));
  }
}

// ===================================================
// 이미지(PNG) 캡처 및 다운로드 기능
// ===================================================
function downloadCanvasAsPng(canvas, fileName) {
  const link = document.createElement("a");
  link.download = fileName;
  link.href = canvas.toDataURL("image/png");
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

function downloadDataUrl(dataUrl, fileName) {
  const link = document.createElement("a");
  link.download = fileName;
  link.href = dataUrl;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

async function exportViewAsImage(sectionId, defaultPrefix) {
  const section = document.getElementById(sectionId);
  if (!section) return;

  let sessionSuffix = "ALL";
  if (selectedSessionIndex >= 0 && loadedSessions[selectedSessionIndex]) {
    sessionSuffix = loadedSessions[selectedSessionIndex].sessionId.substring(0, 8);
  } else if (loadedSessions.length === 1 && loadedSessions[0]) {
    sessionSuffix = loadedSessions[0].sessionId.substring(0, 8);
  }

  const fileName = `Playtest_${defaultPrefix}_${sessionSuffix}.png`;
  const buttons = section.querySelectorAll(".pdf-btn, .img-btn");
  buttons.forEach(b => b.style.visibility = "hidden");

  try {
    if (typeof html2canvas === "function") {
      if (sectionId === "view4Section") redrawCurrentChart();

      const canvas = await html2canvas(section, {
        backgroundColor: "#1e293b",
        scale: 2, // 2x Retina 고해상도 캡처
        useCORS: true,
        logging: false
      });

      downloadCanvasAsPng(canvas, fileName);
    } else {
      const innerCanvas = section.querySelector("canvas");
      if (innerCanvas) {
        const dataUrl = innerCanvas.toDataURL("image/png");
        downloadDataUrl(dataUrl, fileName);
      } else {
        alert("이미지 캡처 모듈(html2canvas)을 불러올 수 없습니다.");
      }
    }
  } catch (err) {
    console.error("이미지 저장 실패:", err);
    alert("이미지 저장 중 오류가 발생했습니다: " + err.message);
  } finally {
    buttons.forEach(b => b.style.visibility = "");
  }
}

async function exportAllViewsAsImages() {
  const btn = document.getElementById("imgAllViewsBtn");
  const originalText = btn ? btn.innerText : "";
  if (btn) btn.innerText = "⏳ 이미지 생성 중...";

  try {
    await exportViewAsImage("view2Section", "View2_Heatmap");
    await new Promise(r => setTimeout(r, 450));
    await exportViewAsImage("view3Section", "View3_Ranking");
    await new Promise(r => setTimeout(r, 450));
    await exportViewAsImage("view4Section", "View4_Timeline");
  } finally {
    if (btn) btn.innerText = originalText;
  }
}

function setupImageExportEvents() {
  const btnView2 = document.getElementById("imgView2Btn");
  if (btnView2) {
    btnView2.addEventListener("click", () => exportViewAsImage("view2Section", "View2_Heatmap"));
  }

  const btnView3 = document.getElementById("imgView3Btn");
  if (btnView3) {
    btnView3.addEventListener("click", () => exportViewAsImage("view3Section", "View3_Ranking"));
  }

  const btnView4 = document.getElementById("imgView4Btn");
  if (btnView4) {
    btnView4.addEventListener("click", () => exportViewAsImage("view4Section", "View4_Timeline"));
  }

  const btnAll = document.getElementById("imgAllViewsBtn");
  if (btnAll) {
    btnAll.addEventListener("click", () => exportAllViewsAsImages());
  }
}

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
