﻿using Channels.Networking.Libuv;
using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RedisCore
{
    public class Program
    {
        const int PipelinedCount = 5000000, RequestResponseCount = 100000,
            BatchSize = 1000, BatchCount = PipelinedCount / BatchSize;
        public static void Main()
        {
           
            Thread.CurrentThread.Name = "Main";
            using (var thread = new UvThread())
            using (var conn = new RedisConnection())
            {
                conn.Connect(thread, new IPEndPoint(IPAddress.Loopback, 6379));

                Thread.Sleep(1000);
                if(conn.IsConnected)
                {
                    Console.WriteLine("RedisCore (bits of StackExchange.Redis with libuv/channels) Connected successfully");
                }
                else
                {
                    Console.WriteLine("Failed to connect; is redis running?");
                    return;
                }

                Console.WriteLine($"Sending {PipelinedCount} pings synchronously fire-and-forget (pipelined) ...");
                int oldOut = conn.OutCount, oldIn = conn.InCount, oldFlush = conn.FlushCount;
                var timer = Stopwatch.StartNew();
                // starting at 1 so that we can wait on the last one and still send the right amount
                for(int i = 1; i < PipelinedCount; i++) conn.Ping(fireAndForget: true);
                conn.Ping(); // block
                timer.Stop();
                int outCount = conn.OutCount - oldOut, inCount = conn.InCount - oldIn, flushCount = conn.FlushCount - oldFlush;
                Console.WriteLine($"out: {outCount}, in: {inCount}, flush: {flushCount}, {timer.ElapsedMilliseconds}ms; {((outCount * 1000.0) / timer.ElapsedMilliseconds):F0} ops/s");

                Console.WriteLine($"Sending {(BatchSize * BatchCount)+1} pings synchronously fire-and-forget ({BatchCount} batches of {BatchSize}) ...");
                oldOut = conn.OutCount;
                oldIn = conn.InCount;
                oldFlush = conn.FlushCount;
                timer = Stopwatch.StartNew();

                for (int i = 0; i < BatchCount; i++)
                {
                    var batch = conn.CreateBatch();
                    for (int j = 0; j < BatchSize; j++)
                    {
                        batch.PingAysnc(true);
                    }
                    batch.Execute();
                }
                conn.Ping(); // block
                timer.Stop();
                outCount = conn.OutCount - oldOut;
                inCount = conn.InCount - oldIn;
                flushCount = conn.FlushCount - oldFlush;
                Console.WriteLine($"out: {outCount}, in: {inCount}, flush: {flushCount}, {timer.ElapsedMilliseconds}ms; {((outCount * 1000.0) / timer.ElapsedMilliseconds):F0} ops/s");

                Console.WriteLine($"Sending {RequestResponseCount} pings synchronously req/resp/req/resp/...");
                oldOut = conn.OutCount;
                oldIn = conn.InCount;
                oldFlush = conn.FlushCount;
                timer = Stopwatch.StartNew();
                for (int i = 0; i < RequestResponseCount; i++) conn.Ping();
                timer.Stop();
                outCount = conn.OutCount - oldOut;
                inCount = conn.InCount - oldIn;
                flushCount = conn.FlushCount - oldFlush;
                Console.WriteLine($"out: {outCount}, in: {inCount}, flush: {flushCount}, {timer.ElapsedMilliseconds}ms; {((outCount * 1000.0) / timer.ElapsedMilliseconds):F0} ops/s");

                PingAsync(conn);

                
                Console.ReadKey();
            }
            
        }

        private static async void PingAsync(RedisConnection conn)
        {
            Console.WriteLine($"Sending {PipelinedCount} pings asynchronously fire-and-forget (pipelined) ...");
            int oldOut = conn.OutCount, oldIn = conn.InCount, oldFlush = conn.FlushCount;
            var timer = Stopwatch.StartNew();
            // starting at 1 so that we can wait on the last one and still send the right amount
            for (int i = 1; i < PipelinedCount; i++) await conn.PingAsync(fireAndForget: true);
            await conn.PingAsync(); // block
            timer.Stop();
            int outCount = conn.OutCount - oldOut, inCount = conn.InCount - oldIn, flushCount = conn.FlushCount - oldFlush;
            Console.WriteLine($"out: {outCount}, in: {inCount}, flush: {flushCount}, {timer.ElapsedMilliseconds}ms; {((outCount * 1000.0) / timer.ElapsedMilliseconds):F0} ops/s");

            Console.WriteLine($"Sending {(BatchSize * BatchCount) + 1} pings asynchronously fire-and-forget ({BatchCount} batches of {BatchSize}) ...");
            oldOut = conn.OutCount;
            oldIn = conn.InCount;
            oldFlush = conn.FlushCount;
            timer = Stopwatch.StartNew();

            Task ignored = null;
            for (int i = 0; i < BatchCount; i++)
            {
                var batch = conn.CreateBatch();
                for (int j = 0; j < BatchSize; j++)
                {
                    ignored = batch.PingAysnc(true);
                }
                await batch.ExecuteAsync();
            }
            await conn.PingAsync(); // block
            timer.Stop();
            outCount = conn.OutCount - oldOut;
            inCount = conn.InCount - oldIn;
            flushCount = conn.FlushCount - oldFlush;
            Console.WriteLine($"out: {outCount}, in: {inCount}, flush: {flushCount}, {timer.ElapsedMilliseconds}ms; {((outCount * 1000.0) / timer.ElapsedMilliseconds):F0} ops/s");


            Console.WriteLine($"Sending {RequestResponseCount} pings asynchronously req/resp/req/resp/...");
            oldOut = conn.OutCount;
            oldIn = conn.InCount;
            oldFlush = conn.FlushCount;
            timer = Stopwatch.StartNew();
            for (int i = 0; i < RequestResponseCount; i++) await conn.PingAsync();
            timer.Stop();
            outCount = conn.OutCount - oldOut;
            inCount = conn.InCount - oldIn;
            flushCount = conn.FlushCount - oldFlush;
            Console.WriteLine($"out: {outCount}, in: {inCount}, flush: {flushCount}, {timer.ElapsedMilliseconds}ms; {((outCount * 1000.0) / timer.ElapsedMilliseconds):F0} ops/s");
        }

        [Conditional("DEBUG")]
        internal static void WriteStatus(string message)
        {
            var thread = Thread.CurrentThread;
            Console.WriteLine($"[{thread.ManagedThreadId}:{thread.Name}] {message}");
        }

        internal static void WriteError(Exception ex, [CallerMemberName] string caller = null)
        {
            Console.Error.WriteLine($"{caller} threw {ex.GetType().Name}");
            Console.Error.WriteLine(ex.Message);
        }
    }
    


  
}
