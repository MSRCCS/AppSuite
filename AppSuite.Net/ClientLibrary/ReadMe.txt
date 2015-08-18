/// By Jin Li

1. add C# Portable Common Library, target .Net version 4.0.3. (remove silverlight)
2. install NuGet packaget BCL async
3. then install NuGet package HttpClient ( see http://stackoverflow.com/questions/19439564/method-error-cannot-await-system-threading-tasks-task-from-await-and-async-pr, 
   you need to first install BCL async and HttpClient to get it work). 
