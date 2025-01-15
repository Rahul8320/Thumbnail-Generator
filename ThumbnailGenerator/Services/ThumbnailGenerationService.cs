
using System.Collections.Concurrent;
using System.Threading.Channels;
using ThumbnailGenerator.Models;

namespace ThumbnailGenerator.Services;

public class ThumbnailGenerationService(
    ILogger<ThumbnailGenerationService> logger,
    ImageService imageService,
    Channel<ThumbnailGenerationJob> channel,
    ConcurrentDictionary<string, ThumbnailGenerationStatus> statusDictionary) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var jobs = channel.Reader.ReadAllAsync(stoppingToken);

        await foreach (var job in jobs)
        {
            try
            {
                await ProcessBackgroudJob(job);
            }
            catch(OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing thumbnail generation jobs.");
            }
        }
    }

    private async Task ProcessBackgroudJob(ThumbnailGenerationJob job)
    {
        statusDictionary[job.Id] = ThumbnailGenerationStatus.Processing;

        try
        {
            await imageService.GenerateThumbnailsAsync(job.OriginalFilePath, job.FolderPath, job.Id);

            statusDictionary[job.Id] = ThumbnailGenerationStatus.Completed;
        }
        catch (Exception ex)
        {
            statusDictionary[job.Id] = ThumbnailGenerationStatus.Failed;

            logger.LogError(ex, "Failed to process backgroud job");
            throw;
        }
    }
}
