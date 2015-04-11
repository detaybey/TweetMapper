# TweetMapper
140journos is a collaborative organization who aims to do "citizen journalism" where authorized contributors use the same twitter account and publish news all over Turkey.

TweetMapper follows this twitter account, tries to find a meaningfull address information, geocodes it and turns it into coordinates. Then whole tweet list is dumped into an Excel file.

For Geocoding, Yandex's Map API is used. For API access I used @exister's YandexGeocoder library https://github.com/exister/YandexGeocoder

You can customize the code for accessing twitter API, appconfig contains credentials for a dummy app I created on Twitter.
