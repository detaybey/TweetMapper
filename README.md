# TweetMapper
140journos is a collaborative media organization who does citizen journalism where authorized contributors use the same twitter account and publish news all over Turkey. For more about 140journos you can visit http://140journos.com/  (unfortunately I'm not affiliated with them)

140journos tweets have a format, such as first 5 digits are time, then an address information, after that the news block and ends with the mention of the news provider. 

TweetMapper is a simple tool to follows this twitter account, tries to find a meaningfull address information from the address block, geocodes it and turns it into coordinates. Then whole tweet list is dumped into an Excel file. 

For Geocoding, Yandex's Map API is used. For API access I used @exister's YandexGeocoder library https://github.com/exister/YandexGeocoder

For Twitter API Access Tweetinvi is used. More about Tweetinvi on https://tweetinvi.codeplex.com/

You can customize the code for accessing twitter API, appconfig contains credentials for a dummy app I created on Twitter.
