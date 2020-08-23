# AWS Lambda Rss Push Notification

This project consists of:
* Lambda function in .Net Core to get notifications when a new job is available in a xml rss flux
* Schedule by Cloud Watch Rule to be executed every 2min
* Write the results in a S3 json file, just keep the jobns of the week


## Here are the steps : 
* Get the rss flux
* Get the json file from S3
* Compare to see if new jobs are available
* Push Notifications with PushOver to a phone
* Save the last jobs in the json file