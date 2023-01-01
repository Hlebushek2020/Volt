﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VoltBot.Logs;
using VoltBot.Logs.Providers;
using Group = VkNet.Model.Group;

namespace VoltBot.Modules
{
    public class ForwardingPostFromVkByUrlModule : IHandlerModule<MessageCreateEventArgs>
    {
        private readonly VkApi _vkApi = new VkApi();
        private readonly ILogger _defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();
        private readonly Regex _groupExportLink =
            new Regex(@"(?<!\\)https:\/\/vk.com\/wall(-?\d+)_(\d+)", RegexOptions.Compiled);
        private readonly Regex _groupNormalLink =
            new Regex(@"(?<!\\)https:\/\/vk.com\/.*w=wall(-?\d+)_(\d+)", RegexOptions.Compiled);

        public ForwardingPostFromVkByUrlModule()
        {
            _vkApi.Authorize(new ApiAuthParams() { AccessToken = Settings.Settings.Current.VkSecret });
        }

        public async Task Handler(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (_vkApi.IsAuthorized)
            {
                DiscordEmoji deleteEmoji = DiscordEmoji.FromName(sender, ":negative_squared_cross_mark:", false);

                string id = TryGetGroupPostIdFromExportUrl(e.Message.Content);
                if (!string.IsNullOrWhiteSpace(id))
                    await ParseGroupPost(id, deleteEmoji, e.Message, e.Channel);

                id = TryGetGroupPostIdFromRegularUrl(e.Message.Content);
                if (!string.IsNullOrWhiteSpace(id))
                    await ParseGroupPost(id, deleteEmoji, e.Message, e.Channel);
            }
        }

        private async Task ParseGroupPost(string postId, DiscordEmoji deleteEmoji, DiscordMessage originalMessage,
            DiscordChannel channel)
        {
            // Group id starts from -
            bool isGroup = postId[0] == '-';

            #region Validation
            WallGetObject post = null;
            try
            {
                post = await _vkApi.Wall.GetByIdAsync(new[] { postId }, true);
            }
            catch (Exception e)
            {
                _defaultLogger.LogWarning(
                    $"Ошибка парсинга поста из группы VK. id: {postId}, expetion: {e.Message} {e.Source} {e.StackTrace}");
                return;
            }

            if (post?.WallPosts == null || post.WallPosts.Count == 0)
            {
                _defaultLogger.LogDebug($"Не удалось получить пост VK по ссылке. id: {postId}");
                return;
            }

            Post wallPost = post.WallPosts.FirstOrDefault();
            Post sourcePost = wallPost;
            if (wallPost is null)
            {
                _defaultLogger.LogDebug($"Не удалось получить пост VK по из коллекции WallPosts. id: {postId}");
                return;
            }
            #endregion

            #region Main content
            string postMessage = wallPost.Text;
            StringBuilder repostInfo = new StringBuilder();
            // Repost handle
            if (wallPost.CopyHistory != null && wallPost.CopyHistory.Count != 0)
            {
                IReadOnlyCollection<Group> historyGroups = await _vkApi.Groups.GetByIdAsync(
                    wallPost.CopyHistory.Select(x => Math.Abs((long) x.OwnerId).ToString())
                        .Append(sourcePost.FromId.ToString().Replace("-", string.Empty))
                        .ToList(), null, null);

                repostInfo.AppendLine($"{(string.IsNullOrEmpty(wallPost.Text) ? string.Empty : wallPost.Text)}");

                for (int i = 0; i < wallPost.CopyHistory.Count; i++)
                {
                    Post repost = wallPost.CopyHistory[i];
                    repostInfo.AppendLine(
                        $"{new string('➦', i + 1)} *repost from [**{historyGroups.ElementAt(i).Name}**](http://vk.com/wall{repost.FromId}_{repost.Id})*");
                    if (!string.IsNullOrEmpty(repost.Text))
                        repostInfo.AppendLine(repost.Text);
                    repostInfo.AppendLine();
                }

                wallPost = wallPost.CopyHistory.Last();
            }
            #endregion

            #region Author
            string authorName;
            string authorUrl;
            string authorIconUrl;

            if (isGroup)
            {
                Group group = post.Groups.FirstOrDefault();
                if (group == null)
                {
                    _defaultLogger.LogInformation(
                        $"Не удалось получить информацию об авторе поста VK {wallPost.FromId}");
                    return;
                }

                authorName = group.Name;
                authorUrl = $"http://vk.com/wall{sourcePost.OwnerId}_{sourcePost.Id}";
                authorIconUrl = group.Photo50.AbsoluteUri;
            }
            else
            {
                User user = post.Profiles.FirstOrDefault();
                if (user == null)
                {
                    _defaultLogger.LogInformation(
                        $"Не удалось получить информацию об авторе поста VK {wallPost.FromId}");
                    return;
                }

                authorName = user.FirstName + " " + user.LastName;
                authorUrl = $"http://vk.com/wall{sourcePost.OwnerId}_{sourcePost.Id}";
                authorIconUrl = user.Photo50.AbsoluteUri;
            }
            #endregion

            #region Attachments
            bool hasImage = false;
            int imgCount = 0;
            List<string> imageUrls = new List<string>();
            List<Tuple<string, string>> fields = new List<Tuple<string, string>>();
            StringBuilder videoUrls = new StringBuilder();
            foreach (Attachment attachment in wallPost.Attachments)
            {
                switch (attachment.Type.FullName)
                {
                    case "VkNet.Model.Attachments.Photo":
                        Photo photo = attachment.Instance as Photo;

                        var size = photo.Sizes.First(x =>
                            x.Width == photo.Sizes.Max(x => x.Width) && x.Height == photo.Sizes.Max(x => x.Height));

                        imageUrls.Add(size.Url.AbsoluteUri);

                        break;

                    case "VkNet.Model.Attachments.Video":
                        Video video = attachment.Instance as Video;

                        videoUrls.Append($"[[**видео**](https://vk.com/video{video.OwnerId}_{video.Id})] ");

                        break;

                    case "VkNet.Model.Attachments.Poll":
                        Poll poll = attachment.Instance as Poll;

                        Tuple<string, string> strPoll = new Tuple<string, string>(
                            poll.Question,
                            string.Join(' ',
                                poll.Answers
                                    .Select(x => $"**{x.Text}** - {x.Votes} ({x.Rate:#.##}%)\n")));

                        fields.Add(strPoll);

                        break;

                    default:
                        _defaultLogger.LogDebug($"Неизвестное vk вложение: {attachment.Type.Name}");
                        break;
                }
            }
            #endregion

            #region Constructing message
            List<DiscordMessageBuilder> messages = new List<DiscordMessageBuilder>();
            List<DiscordEmbedBuilder> finalEmbeds = new List<DiscordEmbedBuilder>();

            int firstEmbedWithImageIndex = 0; // also if this index is NOT 0 the message is potentially long 

            if (repostInfo.Length + postMessage.Length + videoUrls.Length > 4096)
            {
                // Split message in 3 embeds, where:
                // 1 embed: author and repost info
                // 2 embed: content
                // 3 embed: attachments, likes, reposts and other info

                // 1-st embed
                if (repostInfo.Length != 0)
                {
                    finalEmbeds.Add(new DiscordEmbedBuilder()
                        .WithDescription(repostInfo.ToString()));
                }

                // 2-nd embed can be splitted in more embeds. Each one contains post text, splitted in chunks
                List<string> messageChunks = new List<string>();

                if (postMessage.Length >= 4096)
                {
                    // Split post text in chunks by spaces
                    int startIndex = 0,
                        endIndex = 0;

                    do
                    {
                        endIndex += 4096;

                        do
                        {
                            endIndex--;
                        } while (postMessage[endIndex] != ' ');

                        string strChunk = postMessage.Substring(startIndex, endIndex);
                        messageChunks.Add(strChunk);

                        startIndex = endIndex;
                    } while (postMessage.Length - endIndex > 4096);

                    string finalStrChunk = postMessage.Substring(endIndex);
                    messageChunks.Add(finalStrChunk);
                }
                else
                {
                    messageChunks.Add(postMessage);
                }

                foreach (string chunk in messageChunks)
                {
                    finalEmbeds.Add(new DiscordEmbedBuilder()
                        .WithDescription(chunk));
                }

                // 3rd embed
                if (videoUrls.Length != 0)
                {
                    finalEmbeds.Add(new DiscordEmbedBuilder()
                        .WithDescription(videoUrls.ToString()));
                }

                firstEmbedWithImageIndex = finalEmbeds.Count - 1;
            }
            else
            {
                StringBuilder concatedPostMessage = new StringBuilder();

                if (repostInfo.Length != 0)
                {
                    concatedPostMessage.Append(repostInfo);
                }

                concatedPostMessage.Append(postMessage);

                if (videoUrls.Length != 0)
                {
                    concatedPostMessage.Append(videoUrls);
                }

                finalEmbeds.Add(new DiscordEmbedBuilder()
                    .WithDescription(concatedPostMessage.ToString()));
            }

            // In case, when post length is less than 4096 symbols topBuilder and bottomBuilder will be the same 
            DiscordEmbedBuilder topBuilder = finalEmbeds.First();
            topBuilder.WithAuthor(authorName, authorUrl, authorIconUrl);

            if (imageUrls.Count != 0)
            {
                finalEmbeds[firstEmbedWithImageIndex]
                    .WithImageUrl(imageUrls[0])
                    .WithUrl($"http://vk.com/wall{postId}");

                // If embed contains more then 1 image, the top embed should contain image url too
                for (int i = 1; i < imageUrls.Count; i++)
                    finalEmbeds.Add(new DiscordEmbedBuilder()
                        .WithImageUrl(imageUrls[i])
                        .WithUrl($"http://vk.com/wall{postId}"));
            }

            // bottomBuilder is last one, if:
            // - there's more then 4 images
            // - message is potentially very long
            var bottomBuilder = finalEmbeds.Count > 4 || firstEmbedWithImageIndex != 0
                ? finalEmbeds.Last()
                : finalEmbeds.First();

            if (fields.Count != 0)
                foreach (var field in fields)
                    bottomBuilder.AddField(field.Item1, field.Item2);

            bottomBuilder
                .WithFooter(
                    $"Лайков: {sourcePost.Likes.Count}, Репостов: {sourcePost.Reposts.Count}, Просмотров: {sourcePost.Views.Count}",
                    @"https://vk.com/images/icons/favicons/fav_logo.ico")
                .WithTimestamp(sourcePost.Date);

            // The sum characters limit per message (including embed description, fields etc.) is 6000.
            // We'll just post potentially long embeds in seperate messages
            // If there's a repost info - we'll merge it with first msg
            if (firstEmbedWithImageIndex != 0)
            {
                var startIndex = 0;

                if (repostInfo.Length != 0)
                {
                    messages.Add(new DiscordMessageBuilder()
                        .AddEmbeds(finalEmbeds.Take(2).Select(x => x.Build()).ToList()));

                    startIndex = 2;
                }

                for (int i = startIndex; i < firstEmbedWithImageIndex; i++)
                {
                    messages.Add(new DiscordMessageBuilder()
                        .AddEmbed(finalEmbeds[i].Build()));
                }
            }

            for (int i = firstEmbedWithImageIndex; i < finalEmbeds.Count; i += 4)
                messages.Add(new DiscordMessageBuilder()
                    .AddEmbeds(finalEmbeds.Skip(i).Take(4).Select(x => x.Build()).ToList()));

            bool firstMsg = true;
            foreach (DiscordMessageBuilder sendingMessage in messages)
            {
                if (firstMsg)
                {
                    DiscordMessage sentMesage = await originalMessage.RespondAsync(sendingMessage);
                    await sentMesage.CreateReactionAsync(deleteEmoji);
                    firstMsg = false;
                    continue;
                }

                await channel.SendMessageAsync(sendingMessage);
            }
            #endregion
        }

        public string TryGetGroupPostIdFromExportUrl(string msg)
        {
            Match match = _groupExportLink.Match(msg);
            return match.Groups.Count != 3 ? null : $"{match.Groups[1].Value}_{match.Groups[2].Value}";
        }

        public string TryGetGroupPostIdFromRegularUrl(string msg)
        {
            Match match = _groupNormalLink.Match(msg);
            return match.Groups.Count != 3 ? null : $"{match.Groups[1].Value}_{match.Groups[2].Value}";
        }
    }
}