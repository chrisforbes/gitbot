gitbot(1)
=========

NAME
	gitbot -- The (slightly less) stupid content announcer
	
SYNOPSIS
	`gitbot	[--socks] [--whisper]`
	`mono gitbot [--socks] [--whisper]`
	
	`@add <alias> <github user/repo>`	-- start tracking a repo
	`@rm <alias>`						-- stop tracking a repo
	`@status <alias>`					-- shows the status of a repo, or all repos with the alias *
	`@repolist`							-- list the repos
	`@help`								-- list accepted commands

DESCRIPTION
	Announces commits pushed to github, and tries to figure out
	what the pusher actually *did* wrt branches etc.

OPTIONS
	`--socks`
		Connects via a SOCKS5 proxy on localhost:1080. This is
		useful for tunneling through SSH if you need to do that.
	`--whisper`
		Responds to commands directly to user, rather than in
		the channel. Still posts updates to channel.
	
BUGS
	Various cases could be interpreted more cleverly -- rebases, 
		combinations of history actions.
	It's also possible to explode, which is bad.
	Most things that should be configurable aren't.

AUTHOR
	[Chris Forbes]: http://github.com/chrisforbes
	[Alli Witheford]: http://github.com/alzeih