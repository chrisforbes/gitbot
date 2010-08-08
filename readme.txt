gitbot(1)
=========

NAME
	gitbot -- The (slightly less) stupid content announcer
	
SYNOPSIS
	`gitbot	[--socks] [--whisper] [--port PORT] [--user-name UserName] [--nick Nick] [--irc-name IRCName] [--bitly-username UserName] [--bitly-key APIKey] --server Server --channel Channel`
	`mono gitbot [--socks] [--whisper] [--port PORT] [--user-name UserName] [--nick Nick] [--irc-name IRCName] [--bitly-username UserName] [--bitly-key APIKey] --server Server --channel Channel`
	
	`@add <alias> <github user/repo>`	-- start tracking a repo
	`@rm <alias>`						-- stop tracking a repo
	`@status <alias>`					-- shows the status of a repo, or all repos with the alias *
	`@repolist`							-- list the repos
	`@help`								-- list accepted commands

DESCRIPTION
	Announces commits pushed to github, and tries to figure out
	what the pusher actually *did* wrt branches etc.

OPTIONS
	`--server Server`
		Sets the IRC Server to connect to. 
	`--channel Channel`
		Sets the IRC Channel to connect to. Must use the # on the
		channel name.
		
	`--socks`
		Connects via a SOCKS5 proxy on localhost:1080. This is
		useful for tunneling through SSH if you need to do that.
	`--whisper`
		Responds to commands directly to user, rather than in
		the channel. Still posts updates to channel.
	`--port PORT`
		Sets the port to connect to the IRC Server. Default is 6667.
	`--user-name UserName`
		Sets the username of the bot. Default is pizzabot.
	`--nick Nick`
		Sets the nick of the bot. Default is pizzabot.
	`--irc-name IRCName`
		Sets the real name of the bot according to IRC. Default is PizzaBot.
	`--bitly-username UserName`
		Sets the username for bitly url shortening support
	`--bitly-key ApiKey`
		Sets the api key for bitly url shortening support
	
BUGS
	Various cases could be interpreted more cleverly -- rebases, 
		combinations of history actions.
	It's also possible to explode, which is bad.
	Most things that should be configurable aren't.

AUTHOR
	[Chris Forbes]: http://github.com/chrisforbes
	[Alli Witheford]: http://github.com/alzeih